using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var user = await LoadUserAsync();
        var roles = await _users.GetRolesAsync(user);
        var assignment = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .Include(a => a.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.MoveOutDate == null);

        ViewBag.Roles = roles;
        ViewBag.Assignment = assignment;
        ViewBag.LinkedAccounts = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.PersonId == user.PersonId && u.Id != user.Id)
            .ToListAsync();
        ViewBag.Vehicles = await _db.ResidentVehicles.Where(v => v.UserId == user.Id).OrderBy(v => v.RegistrationNumber).ToListAsync();
        return View(user);
    }

    [Authorize(Roles = AppRoles.Student + "," + AppRoles.Staff)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVehicle(VehicleViewModel model)
    {
        var user = await LoadUserAsync();
        if (!ModelState.IsValid) { TempData["Error"] = "Enter a valid vehicle registration."; return RedirectToAction(nameof(Index)); }
        if (await _db.ResidentVehicles.CountAsync(v => v.UserId == user.Id) >= 3)
        { TempData["Error"] = "You can register up to three vehicles."; return RedirectToAction(nameof(Index)); }
        var plate = model.RegistrationNumber.Trim().ToUpperInvariant();
        if (await _db.ResidentVehicles.AnyAsync(v => v.UserId == user.Id && v.RegistrationNumber == plate))
        { TempData["Error"] = "That vehicle is already registered."; return RedirectToAction(nameof(Index)); }
        _db.ResidentVehicles.Add(new ResidentVehicle { UserId = user.Id, RegistrationNumber = plate, MakeAndModel = model.MakeAndModel?.Trim() });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Vehicle registered.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Student + "," + AppRoles.Staff)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveVehicle(int id)
    {
        var user = await LoadUserAsync();
        var vehicle = await _db.ResidentVehicles.FirstOrDefaultAsync(v => v.VehicleId == id && v.UserId == user.Id);
        if (vehicle != null) { _db.ResidentVehicles.Remove(vehicle); await _db.SaveChangesAsync(); TempData["Success"] = "Vehicle removed."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await LoadUserAsync();
        return View(new ProfileEditViewModel
        {
            PersonId = user.PersonId,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            PhoneNumber = user.Person.PhoneNumber,
            Email = user.Email ?? "",
            UserNumber = user.UserNumber,
            DateOfBirth = user.Person.DateOfBirth,
            Gender = user.Person.Gender,
            HasDisability = user.Person.HasDisability,
            DisabilityDetails = user.Person.DisabilityDetails
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await LoadUserAsync();
        user.Person.FirstName = model.FirstName.Trim();
        user.Person.LastName = model.LastName.Trim();
        user.Person.PhoneNumber = model.PhoneNumber;
        user.Person.DateOfBirth = model.DateOfBirth;
        user.Person.Gender = model.Gender;
        user.Person.HasDisability = model.HasDisability;
        user.Person.DisabilityDetails = model.HasDisability ? model.DisabilityDetails?.Trim() : null;
        user.PhoneNumber = model.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<IActionResult> PaymentMethods()
    {
        var user = await LoadUserAsync();
        ViewBag.Cards = await _db.SavedCards.Where(c => c.UserId == user.Id).OrderByDescending(c => c.IsDefault).ToListAsync();
        ViewBag.Banks = await _db.BankAccounts.Where(b => b.UserId == user.Id).OrderByDescending(b => b.IsDefault).ToListAsync();
        return View();
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpGet]
    public async Task<IActionResult> AddCard(string? returnUrl = null)
    {
        var user = await LoadUserAsync();
        return View(new AddCardViewModel { CardholderName = user.Person.FullName, ReturnUrl = returnUrl });
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpGet]
    public async Task<IActionResult> AddBank()
    {
        var user = await LoadUserAsync();

        return View(new AddBankViewModel
        {
            AccountHolder = user.Person.FullName
        });
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCard(AddCardViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var now = DateTime.UtcNow;

        if (model.ExpiryYear < now.Year ||
            (model.ExpiryYear == now.Year &&
             model.ExpiryMonth < now.Month))
        {
            ModelState.AddModelError(
                nameof(model.ExpiryMonth),
                "This card has expired.");

            return View(model);
        }

        var user = await LoadUserAsync();

        var digits = CardHelper.DigitsOnly(model.CardNumber);
        if (!CardHelper.LuhnValid(digits))
        {
            ModelState.AddModelError(nameof(model.CardNumber), "Enter a valid card number.");
            return View(model);
        }

        if (model.ExpiryMonth is < 1 or > 12 || model.Cvv.Length is < 3 or > 4 || model.Cvv.Any(c => !char.IsAsciiDigit(c)))
        {
            ModelState.AddModelError(string.Empty, "Enter valid card details.");
            return View(model);
        }

        var hasExistingCards = await _db.SavedCards
            .AnyAsync(c => c.UserId == user.Id);

        if (model.IsDefault)
        {
            var existingCards = await _db.SavedCards
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            foreach (var existing in existingCards)
            {
                existing.IsDefault = false;
            }
        }

        _db.SavedCards.Add(new SavedCard
        {
            UserId = user.Id,
            CardholderName = model.CardholderName.Trim(),
            Last4 = CardHelper.Last4(digits),
            Brand = CardHelper.DetectBrand(digits),
            ExpiryMonth = model.ExpiryMonth,
            ExpiryYear = model.ExpiryYear,
            Token = "demo_" + CardHelper.Tokenise(digits),

            // First saved card automatically becomes default.
            IsDefault = model.IsDefault || !hasExistingCards,

            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = "Card saved.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
            Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction(nameof(PaymentMethods));
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBank(AddBankViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await LoadUserAsync();

        var digits = CardHelper.DigitsOnly(model.AccountNumber);

        if (digits.Length < 4)
        {
            ModelState.AddModelError(
                nameof(model.AccountNumber),
                "Enter a valid bank account number.");

            return View(model);
        }

        var hasExistingBanks = await _db.BankAccounts
            .AnyAsync(b => b.UserId == user.Id);

        if (model.IsDefault)
        {
            var existingBanks = await _db.BankAccounts
                .Where(b => b.UserId == user.Id)
                .ToListAsync();

            foreach (var existing in existingBanks)
            {
                existing.IsDefault = false;
            }
        }

        _db.BankAccounts.Add(new BankAccount
        {
            UserId = user.Id,
            BankName = model.BankName.Trim(),
            AccountHolder = model.AccountHolder.Trim(),
            AccountNumberLast4 = digits[^4..],
            BranchCode = model.BranchCode.Trim(),
            AccountType = model.AccountType,

            // First bank account automatically becomes default.
            IsDefault = model.IsDefault || !hasExistingBanks,

            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        TempData["Success"] =
            "Bank account saved. Only the last four digits are stored.";

        return RedirectToAction(nameof(PaymentMethods));
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCard(int id)
    {
        var user = await LoadUserAsync();

        var card = await _db.SavedCards
            .FirstOrDefaultAsync(c =>
                c.CardId == id &&
                c.UserId == user.Id);

        if (card == null)
        {
            TempData["Error"] = "Card not found.";
            return RedirectToAction(nameof(PaymentMethods));
        }

        var wasDefault = card.IsDefault;

        _db.SavedCards.Remove(card);

        if (wasDefault)
        {
            var replacement = await _db.SavedCards
                .Where(c =>
                    c.UserId == user.Id &&
                    c.CardId != id)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (replacement != null)
            {
                replacement.IsDefault = true;
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Card removed.";

        return RedirectToAction(nameof(PaymentMethods));
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBank(int id)
    {
        var user = await LoadUserAsync();

        var bank = await _db.BankAccounts
            .FirstOrDefaultAsync(b =>
                b.BankAccountId == id &&
                b.UserId == user.Id);

        if (bank == null)
        {
            TempData["Error"] = "Bank account not found.";
            return RedirectToAction(nameof(PaymentMethods));
        }

        var wasDefault = bank.IsDefault;

        _db.BankAccounts.Remove(bank);

        if (wasDefault)
        {
            var replacement = await _db.BankAccounts
                .Where(b =>
                    b.UserId == user.Id &&
                    b.BankAccountId != id)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            if (replacement != null)
            {
                replacement.IsDefault = true;
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Bank account removed.";

        return RedirectToAction(nameof(PaymentMethods));
    }

    private async Task<ApplicationUser> LoadUserAsync()
    {
        var user = await _users.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        await _db.Entry(user).Reference(u => u.Person).LoadAsync();
        if (!_db.Entry(user).Reference(u => u.EstateAccount).IsLoaded)
            await _db.Entry(user).Reference(u => u.EstateAccount).LoadAsync();
        return user;
    }
}
