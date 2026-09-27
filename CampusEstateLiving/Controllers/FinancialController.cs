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

[Authorize(Roles = $"{AppRoles.FinancialOfficer},{AppRoles.Administrator}")]
public class FinancialController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly NotificationService _notify;

    public FinancialController(ApplicationDbContext db, UserManager<ApplicationUser> users, NotificationService notify)
    {
        _db = db;
        _users = users;
        _notify = notify;
    }

    public IActionResult Index() => RedirectToAction("Dashboard", "Home");

    public async Task<IActionResult> Users(string? q = null)
    {
        var query = _db.Users
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.EstateAccount != null)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u =>
                u.Email!.Contains(q) ||
                u.UserNumber.Contains(q) ||
                u.Person.FirstName.Contains(q) ||
                u.Person.LastName.Contains(q) ||
                (u.EstateAccount != null && u.EstateAccount.AccountNumber.Contains(q)));
        }
        ViewBag.Q = q;
        return View(await query.OrderBy(u => u.Person.LastName).ToListAsync());
    }

    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await _db.Users
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.RoomAssignments).ThenInclude(a => a.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        ViewBag.Payments = user.EstateAccount == null
            ? new List<Payment>()
            : await _db.Payments
                .Include(p => p.Category)
                .Include(p => p.PaymentStatus)
                .Where(p => p.AccountId == user.EstateAccount.AccountId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _db.Users.Include(u => u.Person).Include(u => u.EstateAccount).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        var roles = await _users.GetRolesAsync(user);
        return View(new EditUserViewModel
        {
            UserId = user.Id,
            PersonId = user.PersonId,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            UserNumber = user.UserNumber,
            PhoneNumber = user.Person.PhoneNumber,
            Email = user.Email ?? "",
            IsActive = user.IsActive,
            AccountStatus = user.EstateAccount?.AccountStatus,
            Role = roles.FirstOrDefault() ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _db.Users.Include(u => u.Person).Include(u => u.EstateAccount).FirstOrDefaultAsync(u => u.Id == model.UserId);
        if (user == null) return NotFound();
        user.Person.FirstName = model.FirstName.Trim();
        user.Person.LastName = model.LastName.Trim();
        user.Person.PhoneNumber = model.PhoneNumber;
        user.PhoneNumber = model.PhoneNumber;
        user.UserNumber = model.UserNumber.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        if (user.EstateAccount != null && !string.IsNullOrWhiteSpace(model.AccountStatus))
        {
            user.EstateAccount.AccountStatus = model.AccountStatus;
            user.EstateAccount.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Resident details updated.";
        return RedirectToAction(nameof(UserDetails), new { id = user.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _db.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return View(new ResetUserPasswordViewModel
        {
            UserId = user.Id,
            FullName = user.Person.FullName,
            Email = user.Email ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByIdAsync(model.UserId.ToString());
        if (user == null) return NotFound();
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await _notify.NotifyAsync(user.Id, "Password reset",
            "A financial officer reset the password on this account. Sign in with the new password and change it if you wish.",
            "Account");
        TempData["Success"] = "Password reset.";
        return RedirectToAction(nameof(UserDetails), new { id = user.Id });
    }

    public async Task<IActionResult> Payments(string? status = null, string? q = null)
    {
        var query = _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                p.ReferenceNumber.Contains(q) ||
                p.Account.AccountNumber.Contains(q) ||
                p.Account.User.Person.FirstName.Contains(q) ||
                p.Account.User.Person.LastName.Contains(q));
        }
        ViewBag.Status = status;
        ViewBag.Q = q;
        return View(await query.OrderByDescending(p => p.PaymentDate).ToListAsync());
    }

    public async Task<IActionResult> PaymentDetails(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser).ThenInclude(u => u!.Person)
            .FirstOrDefaultAsync(p => p.PaymentId == id);
        if (payment == null) return NotFound();
        return View(payment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
        => await SetStatus(id, PaymentStatusNames.Confirmed, "Payment confirmed",
            "Your payment has been verified and accepted.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Fail(int id)
        => await SetStatus(id, PaymentStatusNames.Failed, "Payment failed",
            "Your payment could not be verified. Please try another method or contact the estate office.");

    [HttpGet]
    public async Task<IActionResult> Record()
    {
        ViewBag.Accounts = new SelectList(
            await _db.EstateAccounts.Include(a => a.User).ThenInclude(u => u.Person).Include(a => a.User).ThenInclude(u => u.Vehicles)
                .Select(a => new { a.AccountId, Label = a.AccountNumber + " — " + a.User.Person.FirstName + " " + a.User.Person.LastName })
                .ToListAsync(), "AccountId", "Label");
        var categories = await _db.PaymentCategories.OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
        var model = new PaymentCreateViewModel { PaymentMethod = PaymentMethods.Eft };
        model.AvailableTariffs = await _db.Tariffs.Include(t => t.Category).Where(t => t.IsActive).OrderBy(t => t.Category.CategoryName).ToListAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(int accountId, PaymentCreateViewModel model)
    {
        ViewBag.Accounts = new SelectList(
            await _db.EstateAccounts.Include(a => a.User).ThenInclude(u => u.Person).Include(a => a.User).ThenInclude(u => u.Vehicles)
                .Select(a => new { a.AccountId, Label = a.AccountNumber + " — " + a.User.Person.FirstName + " " + a.User.Person.LastName })
                .ToListAsync(), "AccountId", "Label");
        ViewBag.Categories = new SelectList(await _db.PaymentCategories.ToListAsync(), "CategoryId", "CategoryName");
        model.AvailableTariffs = await _db.Tariffs.Include(t => t.Category).Where(t => t.IsActive).OrderBy(t => t.Category.CategoryName).ToListAsync();
        var accountForRecord = await _db.EstateAccounts.Include(a => a.User).ThenInclude(u => u.Vehicles).FirstOrDefaultAsync(a => a.AccountId == accountId);
        if (accountForRecord == null) ModelState.AddModelError(string.Empty, "Select a valid resident.");
        if (!new[] { PaymentMethods.Card, PaymentMethods.Eft, PaymentMethods.Cash }.Contains(model.PaymentMethod)) ModelState.AddModelError(nameof(model.PaymentMethod), "Select a supported payment method.");
        if (model.PaymentMethod == PaymentMethods.Card) ModelState.AddModelError(nameof(model.PaymentMethod), "Card payments require a configured payment gateway. Record an electronic payment or cash payment instead.");
        if (model.PaymentMethod == PaymentMethods.Eft && !(User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.FinancialOfficer))) ModelState.AddModelError(nameof(model.PaymentMethod), "You cannot verify EFT payments from this screen.");
        var tariffId = model.TariffId.GetValueOrDefault();
        if (tariffId > 0)
        {
            var tariff = await _db.Tariffs.FirstOrDefaultAsync(t => t.TariffId == tariffId && t.IsActive);
            if (tariff == null) ModelState.AddModelError(nameof(model.TariffId), "Select an active tariff.");
            else
            {
                model.CategoryId = tariff.CategoryId; model.Amount = tariff.Amount;
                if (await _db.PaymentCategories.Where(c => c.CategoryId == tariff.CategoryId).Select(c => c.CategoryName == PaymentCategoryNames.Parking).FirstOrDefaultAsync() && (accountForRecord?.User.Vehicles.Count ?? 0) < 2)
                    ModelState.AddModelError(nameof(model.TariffId), "Paid parking requires at least two registered vehicles; one vehicle is included.");
            }
        }
        else ModelState.AddModelError(nameof(model.TariffId), "Select a valid active tariff.");
        if (!ModelState.IsValid) return View(model);

        var officerId = _users.GetUserId(User);
        var officer = !int.TryParse(officerId, out var officerUserId)
            ? null
            : await _db.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == officerUserId);
        if (officer == null) return Forbid();
        var confirmed = await _db.PaymentStatuses.SingleAsync(s => s.StatusName == PaymentStatusNames.Confirmed);
        var payment = new Payment
        {
            AccountId = accountId,
            CategoryId = model.CategoryId,
            PaymentStatusId = confirmed.PaymentStatusId,
            ProcessedByUserId = officer.Id,
            ProcessedAt = DateTime.UtcNow,
            Amount = model.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = EstateAccountService.NextPaymentReference(),
            Notes = model.Notes,
            ProcessingNotes = model.PaymentMethod == PaymentMethods.Cash
                ? $"Cash received by {officer.Person?.FullName ?? officer.UserName ?? officer.Email ?? "financial officer"} ({officer.Email})."
                : null
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var account = await _db.EstateAccounts.FindAsync(accountId);
        if (account != null)
        {
            var category = await _db.PaymentCategories.FindAsync(model.CategoryId);
            await _notify.NotifyAsync(account.UserId, "Payment recorded",
                $"A financial officer recorded a {category?.CategoryName} payment of {payment.Amount:C}. Reference {payment.ReferenceNumber}.",
                "Payment Confirmed", payment.PaymentId);
        }

        TempData["Success"] = "Payment recorded and confirmed.";
        return RedirectToAction(nameof(Receipt), new { id = payment.PaymentId });
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser).ThenInclude(u => u!.Person)
            .FirstOrDefaultAsync(p => p.PaymentId == id);
        if (payment == null) return NotFound();
        return View("~/Views/Payments/Receipt.cshtml", payment);
    }

    public async Task<IActionResult> Reports(DateTime? from = null, DateTime? to = null, int? categoryId = null, string? status = null)
    {
        var model = await BuildReportAsync(from, to, categoryId, status);
        ViewBag.Categories = new SelectList(await _db.PaymentCategories.ToListAsync(), "CategoryId", "CategoryName", categoryId);
        return View(model);
    }

    public async Task<IActionResult> Export(DateTime? from = null, DateTime? to = null, int? categoryId = null, string? status = null)
    {
        var model = await BuildReportAsync(from, to, categoryId, status);
        return File(
            ReportCsv.Build(model.Payments),
            "text/csv",
            $"campus-estate-report-{model.From:yyyyMMdd}-{model.To:yyyyMMdd}.csv");
    }

    private async Task<ReportViewModel> BuildReportAsync(DateTime? from, DateTime? to, int? categoryId, string? status)
    {
        var start = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateTime.UtcNow.Date;
        var query = _db.Payments
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end.AddDays(1));
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.PaymentStatus.StatusName == status);
        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        return new ReportViewModel
        {
            From = start,
            To = end,
            CategoryId = categoryId,
            Status = status,
            Payments = payments,
            Total = payments.Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed).Sum(p => p.Amount),
            ByCategory = payments.Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed)
                .GroupBy(p => p.Category.CategoryName).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)),
            ByStatus = payments.GroupBy(p => p.PaymentStatus.StatusName).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<IActionResult> Reviews()
        => View(await _db.Reviews.Include(r => r.User).ThenInclude(u => u.Person)
            .OrderByDescending(r => r.CreatedAt).ToListAsync());

    private async Task<IActionResult> SetStatus(int id, string statusName, string title, string message)
    {
        var payment = await _db.Payments
            .Include(p => p.Account)
            .Include(p => p.PaymentStatus)
            .FirstOrDefaultAsync(p => p.PaymentId == id);
        if (payment == null) return NotFound();
        if (payment.PaymentStatus.StatusName != PaymentStatusNames.Pending)
        {
            TempData["Error"] = "Only pending payments can be processed.";
            return RedirectToAction(nameof(PaymentDetails), new { id });
        }

        var status = await _db.PaymentStatuses.SingleAsync(s => s.StatusName == statusName);
        var officerId = _users.GetUserId(User);
        var officer = officerId == null ? null : await _users.FindByIdAsync(officerId);
        if (officer == null) return Forbid();
        payment.PaymentStatusId = status.PaymentStatusId;
        payment.ProcessedByUserId = officer.Id;
        payment.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _notify.NotifyAsync(payment.Account.UserId, title, $"{message} Reference {payment.ReferenceNumber}.",
            statusName == PaymentStatusNames.Confirmed ? "Payment Confirmed" : "Payment Failed", payment.PaymentId);
        TempData["Success"] = $"{title}.";
        return RedirectToAction(nameof(Receipt), new { id });
    }
}
