using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
public class ReportsController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public ReportsController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ReportDto>> Get(
        DateTime? from = null,
        DateTime? to = null,
        int? categoryId = null,
        string? status = null)
    {
        var start = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateTime.UtcNow.Date;

        var query = _db.Payments
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end.AddDays(1));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus.StatusName == status);

        var payments = await query.ToListAsync();
        var confirmed = payments
            .Where(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed)
            .ToList();

        return Ok(new ReportDto
        {
            From = start,
            To = end,
            PaymentCount = payments.Count,
            ConfirmedTotal = confirmed.Sum(p => p.Amount),
            ByCategory = confirmed
                .GroupBy(p => p.Category.CategoryName)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)),
            ByStatus = payments
                .GroupBy(p => p.PaymentStatus.StatusName)
                .ToDictionary(g => g.Key, g => g.Count())
        });
    }
}
