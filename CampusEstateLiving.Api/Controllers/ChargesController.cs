using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
public class ChargesController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public ChargesController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ChargesDto>> Get()
    {
        var tariffs = await _db.Tariffs
            .AsNoTracking()
            .Include(t => t.Category)
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

        return Ok(new ChargesDto
        {
            CanEdit = User.IsInRole(AppRoles.Administrator),
            Tariffs = tariffs,
            Categories = categories
        });
    }

    [HttpPut("tariffs/{id:int}")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> UpdateTariff(int id, UpdateTariffRequest request)
    {
        var tariff = await _db.Tariffs.FindAsync(id);
        if (tariff == null) return NotFound(new { message = "Tariff not found." });
        if (request.Amount is < 0.01m or > 1_000_000m)
            return BadRequest(new { message = "Tariff amounts must be between R0.01 and R1,000,000.00." });
        if (string.IsNullOrWhiteSpace(request.Unit) || request.Unit.Trim().Length > 40)
            return BadRequest(new { message = "Enter a billing unit of 1 to 40 characters." });

        tariff.Amount = request.Amount;
        tariff.Unit = request.Unit.Trim();
        tariff.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Tariff updated." });
    }

    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        var service = await _db.PaymentCategories.FindAsync(id);
        if (service == null) return NotFound(new { message = "Service not found." });
        if (request.Description?.Length > 250)
            return BadRequest(new { message = "Service descriptions can be at most 250 characters." });

        service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { message = "Service details updated." });
    }
}
