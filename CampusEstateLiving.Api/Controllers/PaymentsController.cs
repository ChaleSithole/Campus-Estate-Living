using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly CampusEstateDbContext _db;
    private readonly EstateAccountService _accounts;
    private readonly NotificationService _notify;

    public PaymentsController(
        CampusEstateDbContext db,
        EstateAccountService accounts,
        NotificationService notify)
    {
        _db = db;
        _accounts = accounts;
        _notify = notify;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetMine(string? status = null)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var query = PaymentsQuery()
            .Where(p => p.Account.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return Ok(payments.Select(PaymentMapper.ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Get(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var payment = await PaymentsQuery()
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null) return NotFound(new { message = "Payment not found." });

        var isStaff = User.IsInRole(AppRoles.Administrator) ||
                      User.IsInRole(AppRoles.FinancialOfficer);

        if (!isStaff && payment.Account.UserId != userId.Value)
            return Forbid();

        return Ok(PaymentMapper.ToDto(payment));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<ActionResult<PaymentDto>> Create(CreatePaymentRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.EstateAccount)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null) return Unauthorized();

        var account = await _accounts.EnsureForResidentAsync(user);
        if (account.AccountStatus != AccountStatuses.Active)
        {
            return BadRequest(new { message = "Your estate account is not active." });
        }

        if (request.PaymentMethod != PaymentMethods.Card &&
            request.PaymentMethod != PaymentMethods.Eft)
        {
            return BadRequest(new { message = "Payment method must be Card or EFT." });
        }

        if (request.PaymentMethod == PaymentMethods.Card)
        {
            return BadRequest(new { message = "Card payment processing requires a configured payment gateway." });
        }

        var tariff = await _db.Tariffs
            .FirstOrDefaultAsync(t => t.TariffId == request.TariffId && t.IsActive);

        if (tariff == null)
        {
            return BadRequest(new { message = "Select a valid payment tariff." });
        }

        if (await _db.PaymentCategories.Where(c => c.CategoryId == tariff.CategoryId).Select(c => c.CategoryName == PaymentCategoryNames.Parking).FirstOrDefaultAsync() && await _db.Users.Where(u => u.Id == user.Id).Select(u => u.Vehicles.Count).FirstOrDefaultAsync() < 2)
            return BadRequest(new { message = "Paid parking is available after registering at least two vehicles; one vehicle is included." });

        var pending = await _db.PaymentStatuses
            .SingleAsync(s => s.StatusName == PaymentStatusNames.Pending);

        var payment = new Payment
        {
            AccountId = account.AccountId,
            CategoryId = tariff.CategoryId,
            PaymentStatusId = pending.PaymentStatusId,
            Amount = tariff.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = EstateAccountService.NextPaymentReference(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            SavedCardId = request.PaymentMethod == PaymentMethods.Card
                ? request.SavedCardId
                : null
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var category = await _db.PaymentCategories.FindAsync(payment.CategoryId);
        await _notify.NotifyAsync(
            user.Id,
            "Payment submitted",
            $"Your {category?.CategoryName} payment of {payment.Amount:C} is pending verification. Reference {payment.ReferenceNumber}.",
            "Payment Pending",
            payment.PaymentId);

        var created = await PaymentsQuery()
            .FirstAsync(p => p.PaymentId == payment.PaymentId);

        return CreatedAtAction(nameof(Get), new { id = payment.PaymentId }, PaymentMapper.ToDto(created));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var payment = await _db.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.Account)
            .FirstOrDefaultAsync(p => p.PaymentId == id && p.Account.UserId == userId.Value);

        if (payment == null) return NotFound(new { message = "Payment not found." });
        if (payment.PaymentStatus.StatusName != PaymentStatusNames.Pending)
        {
            return BadRequest(new { message = "Only pending payments can be cancelled." });
        }

        var cancelled = await _db.PaymentStatuses
            .SingleAsync(s => s.StatusName == PaymentStatusNames.Cancelled);

        payment.PaymentStatusId = cancelled.PaymentStatusId;
        await _db.SaveChangesAsync();
        await _notify.NotifyAsync(
            userId.Value,
            "Payment cancelled",
            $"Payment {payment.ReferenceNumber} was cancelled.",
            "Payment Cancelled",
            payment.PaymentId);

        return Ok(new { message = "Payment cancelled." });
    }

    [HttpGet("all")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll(string? status = null)
    {
        var query = PaymentsQuery();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return Ok(payments.Select(PaymentMapper.ToDto));
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    public Task<IActionResult> Confirm(int id)
        => SetStatus(id, PaymentStatusNames.Confirmed, "Payment confirmed",
            "Your payment has been verified and accepted.");

    [HttpPost("{id:int}/fail")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    public Task<IActionResult> Fail(int id)
        => SetStatus(id, PaymentStatusNames.Failed, "Payment failed",
            "Your payment could not be verified. Please try another method or contact the estate office.");

    [HttpPost("record")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    public async Task<ActionResult<PaymentDto>> Record(RecordPaymentRequest request)
    {
        var officerId = User.GetUserId();
        if (officerId is null) return Unauthorized();

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than zero." });
        }

        if (request.PaymentMethod is not (PaymentMethods.Eft or PaymentMethods.Cash))
            return BadRequest(new { message = "Choose EFT or Cash." });

        var account = await _db.EstateAccounts.Include(a => a.User).ThenInclude(u => u.Vehicles).FirstOrDefaultAsync(a => a.AccountId == request.AccountId);
        if (account == null)
        {
            return NotFound(new { message = "Estate account not found." });
        }

        var category = await _db.PaymentCategories.FindAsync(request.CategoryId);
        if (category == null) return BadRequest(new { message = "Choose a valid service." });
        if (category.CategoryName == PaymentCategoryNames.Parking && account.User.Vehicles.Count < 2)
            return BadRequest(new { message = "Paid parking requires two registered vehicles; one vehicle is included." });

        if (request.TariffId.HasValue)
        {
            var tariff = await _db.Tariffs.FirstOrDefaultAsync(t => t.TariffId == request.TariffId.Value && t.IsActive);
            if (tariff == null || tariff.CategoryId != request.CategoryId || tariff.Amount != request.Amount)
                return BadRequest(new { message = "The selected tariff or amount is invalid." });
        }

        var confirmed = await _db.PaymentStatuses
            .SingleAsync(s => s.StatusName == PaymentStatusNames.Confirmed);

        var payment = new Payment
        {
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            PaymentStatusId = confirmed.PaymentStatusId,
            ProcessedByUserId = officerId.Value,
            ProcessedAt = DateTime.UtcNow,
            Amount = request.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = EstateAccountService.NextPaymentReference(),
            Notes = request.Notes,
            ProcessingNotes = request.PaymentMethod == PaymentMethods.Cash ? $"Cash received by officer user {officerId.Value}." : null
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        await _notify.NotifyAsync(
            account.UserId,
            "Payment recorded",
            $"A financial officer recorded a {category?.CategoryName} payment of {payment.Amount:C}. Reference {payment.ReferenceNumber}.",
            "Payment Confirmed",
            payment.PaymentId);

        var created = await PaymentsQuery()
            .FirstAsync(p => p.PaymentId == payment.PaymentId);

        return Ok(PaymentMapper.ToDto(created));
    }

    [HttpGet("tariffs")]
    public async Task<ActionResult<IEnumerable<TariffDto>>> Tariffs()
    {
        var tariffs = await _db.Tariffs
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category.CategoryName)
            .ThenBy(t => t.TariffName)
            .Select(t => new TariffDto
            {
                TariffId = t.TariffId,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.CategoryName,
                TariffName = t.TariffName,
                Amount = t.Amount,
                Unit = t.Unit,
                IsActive = t.IsActive
            })
            .ToListAsync();

        return Ok(tariffs);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<PaymentCategoryDto>>> Categories()
    {
        var categories = await _db.PaymentCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .Select(c => new PaymentCategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description
            })
            .ToListAsync();

        return Ok(categories);
    }

    private IQueryable<Payment> PaymentsQuery()
        => _db.Payments
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser).ThenInclude(u => u!.Person)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person);

    private async Task<IActionResult> SetStatus(
        int id,
        string statusName,
        string title,
        string message)
    {
        var officerId = User.GetUserId();
        if (officerId is null) return Unauthorized();

        var payment = await _db.Payments
            .Include(p => p.Account)
            .Include(p => p.PaymentStatus)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null) return NotFound(new { message = "Payment not found." });
        if (payment.PaymentStatus.StatusName != PaymentStatusNames.Pending)
        {
            return BadRequest(new { message = "Only pending payments can be processed." });
        }

        var status = await _db.PaymentStatuses.SingleAsync(s => s.StatusName == statusName);
        payment.PaymentStatusId = status.PaymentStatusId;
        payment.ProcessedByUserId = officerId.Value;
        payment.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notify.NotifyAsync(
            payment.Account.UserId,
            title,
            $"{message} Reference {payment.ReferenceNumber}.",
            statusName == PaymentStatusNames.Confirmed ? "Payment Confirmed" : "Payment Failed",
            payment.PaymentId);

        return Ok(new { message = title + "." });
    }
}
