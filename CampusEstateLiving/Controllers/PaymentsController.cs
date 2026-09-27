using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly NotificationService _notify;
    private readonly EstateAccountService _accounts;

    public PaymentsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        NotificationService notify,
        EstateAccountService accounts)
    {
        _db = db;
        _users = users;
        _notify = notify;
        _accounts = accounts;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var user = await CurrentAsync();
        var account = await _db.EstateAccounts.FirstOrDefaultAsync(a => a.UserId == user.Id);
        if (account == null)
        {
            TempData["Error"] = "No estate account is linked to this login.";
            return RedirectToAction("Dashboard", "Home");
        }

        var query = _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Where(p => p.AccountId == account.AccountId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);

        var list = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        ViewBag.Account = account;
        ViewBag.OutstandingAmount = await _db.Payments
            .Where(p => p.AccountId == account.AccountId && p.PaymentStatus.StatusName == PaymentStatusNames.Pending)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        ViewBag.LastPayment = list.FirstOrDefault();
        ViewBag.RecentPayments = list.Take(5).ToList();
        ViewBag.PaymentStatus = list.FirstOrDefault()?.PaymentStatus.StatusName ?? "No payments yet";
        ViewBag.Status = status;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? categoryId = null)
    {
        var user = await CurrentAsync();
        var account = await _accounts.EnsureForResidentAsync(user);
        var cards = await _db.SavedCards.Where(c => c.UserId == user.Id).ToListAsync();
        var vehicleCount = await _db.ResidentVehicles.CountAsync(v => v.UserId == user.Id);
        var assignment = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.MoveOutDate == null);

        var model = new PaymentCreateViewModel
        {
            CategoryId = categoryId ?? 0,
            AccountNumber = account.AccountNumber,
            RoomLabel = assignment == null ? "Not assigned" : $"{assignment.Room.Property.BuildingName} Room {assignment.Room.RoomNumber}",
            Cards = cards,
            HasCards = cards.Count > 0,
            VehicleCount = vehicleCount,
            SavedCardId = cards.FirstOrDefault(c => c.IsDefault)?.CardId ?? cards.FirstOrDefault()?.CardId,
            Categories = await CategoryOptions(vehicleCount),
            Tariffs = await _db.Tariffs.Include(t => t.Category).Where(t => t.IsActive).OrderBy(t => t.Category.CategoryName).ThenBy(t => t.TariffId).ToListAsync()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        var user = await CurrentAsync();

        var account = await _db.EstateAccounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id);

        if (account == null || account.AccountStatus != AccountStatuses.Active)
        {
            TempData["Error"] = "Your estate account is not active.";
            return RedirectToAction("Dashboard", "Home");
        }

        // Reload server-side data. Never trust posted tariff/category/amount values.
        var vehicleCount = await _db.ResidentVehicles.CountAsync(v => v.UserId == user.Id);
        model.Categories = await CategoryOptions(vehicleCount);
        model.VehicleCount = vehicleCount;

        model.Tariffs = await _db.Tariffs
            .Include(t => t.Category)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category.CategoryName)
            .ThenBy(t => t.TariffId)
            .ToListAsync();

        model.Cards = await _db.SavedCards
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        model.HasCards = model.Cards.Count > 0;
        model.AccountNumber = account.AccountNumber;
        if (model.PaymentMethod is not (PaymentMethods.Card or PaymentMethods.Eft))
            ModelState.AddModelError(nameof(model.PaymentMethod), "Choose a supported payment method.");

        // Card validation
        if (model.PaymentMethod == PaymentMethods.Card)
        {
            if (!model.HasCards)
            {
                TempData["Error"] = "Add a card before paying by card.";

                return RedirectToAction(
                    "AddCard",
                    "Profile",
                    new
                    {
                        returnUrl = Url.Action("Create", "Payments")
                    });
            }

            var cardOk =
                model.SavedCardId.HasValue &&
                await _db.SavedCards.AnyAsync(c =>
                    c.CardId == model.SavedCardId.Value &&
                    c.UserId == user.Id);

            if (!cardOk)
            {
                ModelState.AddModelError(
                    nameof(model.SavedCardId),
                    "Select one of your saved cards.");
            }
        }
        else
        {
            // A saved card must never be attached to a non-card payment.
            model.SavedCardId = null;
        }

        var selectedCategory = await _db.PaymentCategories.FirstOrDefaultAsync(c =>
            c.CategoryId == model.CategoryId &&
            (c.CategoryName != PaymentCategoryNames.Parking || vehicleCount >= 2));
        if (selectedCategory == null)
        {
            var parkingUnavailable = vehicleCount < 2 && await _db.PaymentCategories.AnyAsync(c =>
                c.CategoryId == model.CategoryId && c.CategoryName == PaymentCategoryNames.Parking);
            ModelState.AddModelError(
                nameof(model.CategoryId),
                parkingUnavailable ? "Paid parking requires at least two registered vehicles." : "Choose a valid service.");
        }
        else
        {
            var categoryHasFixedTariff = selectedCategory.CategoryName is PaymentCategoryNames.Water or PaymentCategoryNames.RefuseCollection or PaymentCategoryNames.Parking;
            var tariff = categoryHasFixedTariff
                ? await _db.Tariffs.Where(t => t.CategoryId == selectedCategory.CategoryId && t.IsActive).OrderBy(t => t.TariffId).FirstOrDefaultAsync()
                : model.TariffId.HasValue
                    ? await _db.Tariffs.FirstOrDefaultAsync(t => t.TariffId == model.TariffId.Value && t.IsActive && t.CategoryId == selectedCategory.CategoryId)
                    : null;

            if (tariff == null)
            {
                ModelState.AddModelError(
                    nameof(model.TariffId),
                    "Choose an available room type or usage package.");
            }
            else if (selectedCategory.CategoryName == PaymentCategoryNames.Rent &&
                     !tariff.TariffName.Contains("single room", StringComparison.OrdinalIgnoreCase) &&
                     !tariff.TariffName.Contains("sharing room", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.TariffId), "Choose Single Room or Sharing Room.");
            }
            else
            {
                // Resolve the tariff and total on the server; parking charges once per vehicle after the included vehicle.
                model.TariffId = tariff.TariffId;
                model.CategoryId = tariff.CategoryId;
                model.Amount = selectedCategory.CategoryName == PaymentCategoryNames.Parking
                    ? tariff.Amount * (vehicleCount - 1)
                    : tariff.Amount;
                ModelState.Remove(nameof(model.Amount));
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Notes) &&
            model.Notes.Length > 250)
        {
            ModelState.AddModelError(
                nameof(model.Notes),
                "Notes cannot exceed 250 characters.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cardPayment = model.PaymentMethod == PaymentMethods.Card;
        var paymentStatusName = cardPayment ? PaymentStatusNames.Confirmed : PaymentStatusNames.Pending;
        var paymentStatus = await _db.PaymentStatuses
            .SingleOrDefaultAsync(s => s.StatusName == paymentStatusName);

        if (paymentStatus == null)
        {
            throw new InvalidOperationException(
                $"The {paymentStatusName} payment status has not been configured.");
        }

        var payment = new Payment
        {
            AccountId = account.AccountId,
            CategoryId = model.CategoryId,
            PaymentStatusId = paymentStatus.PaymentStatusId,
            Amount = model.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = EstateAccountService.NextPaymentReference(),
            Notes = string.IsNullOrWhiteSpace(model.Notes)
                ? null
                : model.Notes.Trim(),
            SavedCardId = model.PaymentMethod == PaymentMethods.Card
                ? model.SavedCardId
                : null,
            ProcessedAt = cardPayment ? DateTime.UtcNow : null,
            ProcessingNotes = cardPayment ? "Simulated card authorization for school project. No real funds were transferred." : null
        };

        _db.Payments.Add(payment);

        await _db.SaveChangesAsync();

        var category = await _db.PaymentCategories
            .FindAsync(payment.CategoryId);

        await _notify.NotifyAsync(
            user.Id,
            cardPayment ? "Card payment confirmed" : "Payment submitted",
            cardPayment
                ? $"Your {category?.CategoryName} card payment of {payment.Amount:C} was confirmed. Reference {payment.ReferenceNumber}."
                : $"Your {category?.CategoryName} payment of {payment.Amount:C} is pending verification. Reference {payment.ReferenceNumber}.",
            cardPayment ? "Payment Confirmed" : "Payment Pending",
            payment.PaymentId);

        TempData["Success"] = cardPayment
            ? $"Card payment confirmed. Reference {payment.ReferenceNumber}."
            : $"Payment submitted. Your receipt is ready — reference {payment.ReferenceNumber}.";

        return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await CurrentAsync();
        var payment = await _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser).ThenInclude(u => u!.Person)
            .FirstOrDefaultAsync(p => p.PaymentId == id && p.Account.UserId == user.Id);
        if (payment == null) return NotFound();
        return View(payment);
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await LoadOwnPayment(id);
        if (payment == null) return NotFound();
        ViewBag.Print = true;
        return View(payment);
    }

    public async Task<IActionResult> Statement(int? year = null, int? month = null)
    {
        var user = await CurrentAsync();
        var account = await _db.EstateAccounts.FirstOrDefaultAsync(a => a.UserId == user.Id);
        if (account == null)
        {
            TempData["Error"] = "No estate account is linked to this login.";
            return RedirectToAction("Dashboard", "Home");
        }

        var now = SaTime.Now;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        var start = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var payments = await _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Where(p => p.AccountId == account.AccountId && p.PaymentDate >= start && p.PaymentDate < end)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();

        var assignment = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.MoveOutDate == null);

        return View(new StatementViewModel
        {
            FullName = user.Person.FullName,
            Email = user.Email ?? "",
            UserNumber = user.UserNumber,
            AccountNumber = account.AccountNumber,
            RoomLabel = assignment == null ? "Not assigned" : $"{assignment.Room.Property.BuildingName} Room {assignment.Room.RoomNumber}",
            Year = y,
            Month = m,
            Payments = payments,
            ConfirmedTotal = payments.Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed).Sum(p => p.Amount),
            PendingTotal = payments.Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Pending).Sum(p => p.Amount)
        });
    }

    private async Task<Payment?> LoadOwnPayment(int id)
    {
        var user = await CurrentAsync();
        return await _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser).ThenInclude(u => u!.Person)
            .FirstOrDefaultAsync(p => p.PaymentId == id && p.Account.UserId == user.Id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await CurrentAsync();
        var payment = await _db.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account)
            .FirstOrDefaultAsync(p => p.PaymentId == id && p.Account.UserId == user.Id);
        if (payment == null) return NotFound();
        if (payment.PaymentStatus.StatusName != PaymentStatusNames.Pending)
        {
            TempData["Error"] = "Only pending payments can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var cancelled = await _db.PaymentStatuses.SingleAsync(s => s.StatusName == PaymentStatusNames.Cancelled);
        payment.PaymentStatusId = cancelled.PaymentStatusId;
        await _db.SaveChangesAsync();
        await _notify.NotifyAsync(user.Id, "Payment cancelled",
            $"Payment {payment.ReferenceNumber} was cancelled.", "Payment Cancelled", payment.PaymentId);
        TempData["Success"] = "Payment cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser> CurrentAsync()
    {
        var user = await _users.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        await _db.Entry(user).Reference(u => u.Person).LoadAsync();
        return user;
    }

    private async Task<List<SelectListItem>> CategoryOptions(int vehicleCount = 0)
        => await _db.PaymentCategories
            .Where(c => c.CategoryName != PaymentCategoryNames.Parking || vehicleCount >= 2)
            .OrderBy(c => c.CategoryName == PaymentCategoryNames.Rent ? 0 :
                c.CategoryName == PaymentCategoryNames.Electricity ? 1 :
                c.CategoryName == PaymentCategoryNames.Water ? 2 :
                c.CategoryName == PaymentCategoryNames.Parking ? 3 : 4)
            .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
            .ToListAsync();
}
