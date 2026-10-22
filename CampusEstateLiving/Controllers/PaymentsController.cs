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
        ViewBag.PayableServices = await PayableServicesAsync();
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

    [HttpGet]
    public async Task<IActionResult> All()
    {
        var model = await BuildPayAllModelAsync();
        if (model.Services.Count == 0)
        {
            TempData["Error"] = "No payable services are available for your account yet.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> All(PayAllViewModel model)
    {
        var user = await CurrentAsync();
        var account = await _db.EstateAccounts.FirstOrDefaultAsync(a => a.UserId == user.Id);

        if (account == null || account.AccountStatus != AccountStatuses.Active)
        {
            TempData["Error"] = "Your estate account is not active.";
            return RedirectToAction("Dashboard", "Home");
        }

        var selectedIds = model.Services.Where(s => s.IsSelected).Select(s => s.CategoryId).ToHashSet();
        // Honour the electricity (or other choosable) tariff the user picked on the form.
        var selectedTariffs = model.Services
            .Where(s => s.IsSelected && s.TariffId > 0)
            .GroupBy(s => s.CategoryId)
            .ToDictionary(g => g.Key, g => g.First().TariffId);
        var resolved = await BuildPayAllModelAsync(selectedIds, selectedTariffs);
        resolved.PaymentMethod = model.PaymentMethod;
        resolved.SavedCardId = model.SavedCardId;

        if (resolved.Services.Count == 0)
            ModelState.AddModelError(string.Empty, "Select at least one available service to pay.");

        if (resolved.PaymentMethod is not (PaymentMethods.Card or PaymentMethods.Eft))
            ModelState.AddModelError(nameof(model.PaymentMethod), "Choose a supported payment method.");

        if (resolved.PaymentMethod == PaymentMethods.Card)
        {
            if (!resolved.HasCards)
            {
                TempData["Error"] = "Add a card before paying by card.";
                return RedirectToAction("AddCard", "Profile", new { returnUrl = Url.Action(nameof(All), "Payments") });
            }

            var cardOk = resolved.SavedCardId.HasValue &&
                await _db.SavedCards.AnyAsync(c => c.CardId == resolved.SavedCardId.Value && c.UserId == user.Id);
            if (!cardOk)
                ModelState.AddModelError(nameof(model.SavedCardId), "Select one of your saved cards.");
        }
        else
        {
            resolved.SavedCardId = null;
        }

        // Hard gate: never charge parking unless the user still qualifies (registered vehicles).
        var vehicleCount = await _db.ResidentVehicles.CountAsync(v => v.UserId == user.Id);
        var parkingSelected = resolved.Services.Any(s => s.CategoryName == PaymentCategoryNames.Parking);
        if (parkingSelected && vehicleCount < 2)
        {
            ModelState.AddModelError(string.Empty,
                "Parking is only available when you have at least two registered vehicles. Parking was not charged.");
            resolved.Services = resolved.Services
                .Where(s => s.CategoryName != PaymentCategoryNames.Parking)
                .ToList();
            if (resolved.Services.Count == 0)
                return View(resolved);
        }

        if (!ModelState.IsValid)
            return View(resolved);

        var cardPayment = resolved.PaymentMethod == PaymentMethods.Card;
        var statusName = cardPayment ? PaymentStatusNames.Confirmed : PaymentStatusNames.Pending;
        var status = await _db.PaymentStatuses.SingleAsync(s => s.StatusName == statusName);
        var payments = resolved.Services.Select(service => new Payment
        {
            AccountId = account.AccountId,
            CategoryId = service.CategoryId,
            PaymentStatusId = status.PaymentStatusId,
            Amount = service.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = resolved.PaymentMethod,
            ReferenceNumber = EstateAccountService.NextPaymentReference(),
            Notes = "Paid through pay all services.",
            SavedCardId = cardPayment ? resolved.SavedCardId : null,
            ProcessedAt = cardPayment ? DateTime.UtcNow : null,
            ProcessingNotes = cardPayment ? "Simulated card authorization for school project. No real funds were transferred." : null
        }).ToList();

        _db.Payments.AddRange(payments);
        await _db.SaveChangesAsync();

        await _notify.NotifyAsync(
            user.Id,
            cardPayment ? "Service payments confirmed" : "Service payments submitted",
            cardPayment
                ? $"Your {payments.Count} service payments totaling {payments.Sum(p => p.Amount):C} were confirmed."
                : $"Your {payments.Count} service payments totaling {payments.Sum(p => p.Amount):C} are pending verification.",
            cardPayment ? "Payment Confirmed" : "Payment Pending",
            payments.First().PaymentId);

        TempData["Success"] = cardPayment
            ? $"{payments.Count} service payments confirmed."
            : $"{payments.Count} service payments submitted for verification.";

        return RedirectToAction(nameof(Index));
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

    private async Task<List<PayableServiceViewModel>> PayableServicesAsync()
    {
        var user = await CurrentAsync();
        var vehicleCount = await _db.ResidentVehicles.CountAsync(v => v.UserId == user.Id);
        var categories = await _db.PaymentCategories
            .Include(c => c.Tariffs.Where(t => t.IsActive))
            .ToListAsync();

        return categories
            .OrderBy(c => c.CategoryName == PaymentCategoryNames.Rent ? 0 :
                c.CategoryName == PaymentCategoryNames.Electricity ? 1 :
                c.CategoryName == PaymentCategoryNames.Water ? 2 :
                c.CategoryName == PaymentCategoryNames.Parking ? 3 : 4)
            .Select(c =>
            {
                var tariffs = c.Tariffs.OrderBy(t => t.Amount).ToList();
                var parkingUnavailable = c.CategoryName == PaymentCategoryNames.Parking && vehicleCount < 2;
                var startingAmount = c.CategoryName == PaymentCategoryNames.Parking && tariffs.Count > 0
                    ? tariffs[0].Amount * Math.Max(0, vehicleCount - 1)
                    : tariffs.FirstOrDefault()?.Amount ?? 0m;

                return new PayableServiceViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    StartingAmount = startingAmount,
                    Unit = tariffs.FirstOrDefault()?.Unit ?? "",
                    OptionCount = tariffs.Count,
                    IsAvailable = tariffs.Count > 0 && !parkingUnavailable,
                    AvailabilityMessage = parkingUnavailable
                        ? "Paid parking is available once two or more vehicles are registered."
                        : tariffs.Count == 0 ? "No active tariff is available for this service yet." : null
                };
            })
            .ToList();
    }

    private async Task<PayAllViewModel> BuildPayAllModelAsync(ISet<int>? selectedCategoryIds = null, IReadOnlyDictionary<int, int>? selectedTariffByCategory = null)
    {
        var user = await CurrentAsync();
        var account = await _accounts.EnsureForResidentAsync(user);
        var cards = await _db.SavedCards.Where(c => c.UserId == user.Id).ToListAsync();
        var vehicleCount = await _db.ResidentVehicles.CountAsync(v => v.UserId == user.Id);
        var assignment = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .Include(a => a.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.MoveOutDate == null);

        var categories = await _db.PaymentCategories
            .Include(c => c.Tariffs.Where(t => t.IsActive))
            .ToListAsync();

        var services = new List<PayAllServiceViewModel>();
        foreach (var category in categories)
        {
            var tariffs = category.Tariffs.OrderBy(t => t.Amount).ToList();
            if (tariffs.Count == 0) continue;

            var isElectricity = category.CategoryName == PaymentCategoryNames.Electricity;
            var isParking = category.CategoryName == PaymentCategoryNames.Parking;

            Tariff? tariff = category.CategoryName switch
            {
                PaymentCategoryNames.Rent => assignment?.Room.RoomType.RoomTypeName == RoomTypeNames.Single
                    ? tariffs.FirstOrDefault(t => t.TariffName.Contains("single room", StringComparison.OrdinalIgnoreCase))
                    : assignment?.Room.RoomType.RoomTypeName == RoomTypeNames.Sharing
                        ? tariffs.FirstOrDefault(t => t.TariffName.Contains("sharing room", StringComparison.OrdinalIgnoreCase))
                        : null,
                PaymentCategoryNames.Parking => vehicleCount >= 2 ? tariffs.FirstOrDefault() : null,
                PaymentCategoryNames.Electricity => null, // resolved below from user choice or default
                _ => tariffs.FirstOrDefault()
            };

            List<PayAllTariffOption> electricityOptions = [];
            if (isElectricity)
            {
                electricityOptions = tariffs
                    .OrderBy(t => t.Amount)
                    .Select(t => new PayAllTariffOption
                    {
                        TariffId = t.TariffId,
                        TariffName = t.TariffName,
                        Amount = t.Amount,
                        Unit = t.Unit
                    })
                    .ToList();

                if (electricityOptions.Count == 0)
                    continue;

                int chosenTariffId;
                if (selectedTariffByCategory != null
                    && selectedTariffByCategory.TryGetValue(category.CategoryId, out var postedTariffId)
                    && electricityOptions.Any(o => o.TariffId == postedTariffId))
                {
                    chosenTariffId = postedTariffId;
                }
                else
                {
                    chosenTariffId = electricityOptions[0].TariffId;
                }

                tariff = tariffs.First(t => t.TariffId == chosenTariffId);
            }

            if (tariff == null) continue;

            var amount = isParking
                ? tariff.Amount * (vehicleCount - 1)
                : tariff.Amount;

            if (amount <= 0) continue;

            // Parking is never charged unless the user qualifies (vehicles) AND explicitly selects it.
            var defaultSelected = !isParking && (selectedCategoryIds == null || selectedCategoryIds.Contains(category.CategoryId));
            var explicitlySelected = selectedCategoryIds != null && selectedCategoryIds.Contains(category.CategoryId);

            services.Add(new PayAllServiceViewModel
            {
                CategoryId = category.CategoryId,
                TariffId = tariff.TariffId,
                CategoryName = category.CategoryName,
                TariffName = tariff.TariffName,
                Description = category.Description,
                Amount = amount,
                IsSelected = isParking ? explicitlySelected : defaultSelected,
                AllowsTariffChoice = isElectricity,
                TariffOptions = electricityOptions
            });
        }

        return new PayAllViewModel
        {
            AccountNumber = account.AccountNumber,
            RoomLabel = assignment == null ? "Not assigned" : $"{assignment.Room.Property.BuildingName} Room {assignment.Room.RoomNumber}",
            Cards = cards,
            HasCards = cards.Count > 0,
            SavedCardId = cards.FirstOrDefault(c => c.IsDefault)?.CardId ?? cards.FirstOrDefault()?.CardId,
            Services = services
                .Where(s => selectedCategoryIds == null || s.IsSelected)
                .OrderBy(s => s.CategoryName == PaymentCategoryNames.Rent ? 0 :
                    s.CategoryName == PaymentCategoryNames.Electricity ? 1 :
                    s.CategoryName == PaymentCategoryNames.Water ? 2 :
                    s.CategoryName == PaymentCategoryNames.Parking ? 3 : 4)
                .ToList()
        };
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
