using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize(Roles = AppRoles.FinancialOfficer)]
public class FinancialOfficerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly NotificationService _notify;

    public FinancialOfficerController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        NotificationService notify)
    {
        _db = db;
        _users = users;
        _notify = notify;
    }

    [HttpGet]
    public async Task<IActionResult> Payments(string? status = null)
    {
        var query = _db.Payments
            .Include(p => p.Account)
                .ThenInclude(a => a.User)
                    .ThenInclude(u => u.Person)
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.ProcessedByUser)
                .ThenInclude(u => u!.Person)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p =>
                p.PaymentStatus.StatusName == status);
        }

        var payments = await query
            .OrderByDescending(p =>
                p.PaymentStatus.StatusName == PaymentStatusNames.Pending)
            .ThenByDescending(p => p.PaymentDate)
            .ToListAsync();

        ViewBag.Status = status;

        return View(payments);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Account)
                .ThenInclude(a => a.User)
                    .ThenInclude(u => u.Person)
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser)
                .ThenInclude(u => u!.Person)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null)
            return NotFound();

        return View(payment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        int id,
        string? processingNotes)
    {
        return await ProcessPayment(
            id,
            PaymentStatusNames.Confirmed,
            processingNotes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Fail(
        int id,
        string? processingNotes)
    {
        if (string.IsNullOrWhiteSpace(processingNotes))
        {
            TempData["Error"] =
                "A reason is required when a payment is marked as failed.";

            return RedirectToAction(nameof(Details), new { id });
        }

        return await ProcessPayment(
            id,
            PaymentStatusNames.Failed,
            processingNotes);
    }

    private async Task<IActionResult> ProcessPayment(
    int id,
    string targetStatus,
    string? processingNotes)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var payment = await _db.Payments
            .Include(p => p.Account)
                .ThenInclude(a => a.User)
                    .ThenInclude(u => u.Person)
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null)
            return NotFound();

        // Only a Pending payment may be processed.
        if (payment.PaymentStatus.StatusName !=
            PaymentStatusNames.Pending)
        {
            TempData["Error"] =
                "Only pending payments can be processed.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var status = await _db.PaymentStatuses
            .SingleOrDefaultAsync(s =>
                s.StatusName == targetStatus);

        if (status == null)
        {
            TempData["Error"] =
                $"Payment status '{targetStatus}' was not found.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var officer = await _users.GetUserAsync(User);

        if (officer == null)
            return Challenge();

        payment.PaymentStatusId = status.PaymentStatusId;
        payment.ProcessedByUserId = officer.Id;
        payment.ProcessedAt = DateTime.UtcNow;
        payment.ProcessingNotes =
            string.IsNullOrWhiteSpace(processingNotes)
                ? null
                : processingNotes.Trim();

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        var title =
            targetStatus == PaymentStatusNames.Confirmed
                ? "Payment confirmed"
                : "Payment failed";

        var message =
            targetStatus == PaymentStatusNames.Confirmed
                ? $"Your {payment.Category.CategoryName} payment of " +
                  $"{payment.Amount:C} has been confirmed. " +
                  $"Reference {payment.ReferenceNumber}."
                : $"Your {payment.Category.CategoryName} payment of " +
                  $"{payment.Amount:C} was not accepted. " +
                  $"Reference {payment.ReferenceNumber}.";

        await _notify.NotifyAsync(
            payment.Account.UserId,
            title,
            message,
            "Payment",
            payment.PaymentId);

        TempData["Success"] =
            targetStatus == PaymentStatusNames.Confirmed
                ? $"Payment {payment.ReferenceNumber} confirmed."
                : $"Payment {payment.ReferenceNumber} marked as failed.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }
}