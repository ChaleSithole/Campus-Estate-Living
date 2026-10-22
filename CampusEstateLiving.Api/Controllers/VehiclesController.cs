using System.Text.RegularExpressions;
using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
public class VehiclesController : ControllerBase
{
    private static readonly Regex PlatePattern = new("^[A-Z0-9 -]+$", RegexOptions.Compiled);
    private readonly CampusEstateDbContext _db;

    public VehiclesController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> Mine()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var vehicles = await _db.ResidentVehicles
            .AsNoTracking()
            .Where(v => v.UserId == userId.Value)
            .OrderBy(v => v.RegistrationNumber)
            .Select(v => new VehicleDto
            {
                VehicleId = v.VehicleId,
                RegistrationNumber = v.RegistrationNumber,
                MakeAndModel = v.MakeAndModel,
                RegisteredAt = v.RegisteredAt
            })
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Add(AddVehicleRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var plate = (request.RegistrationNumber ?? string.Empty).Trim().ToUpperInvariant();
        if (plate.Length < 2 || plate.Length > 12 || !PlatePattern.IsMatch(plate))
            return BadRequest(new { message = "Enter a valid vehicle registration." });

        if (await _db.ResidentVehicles.CountAsync(v => v.UserId == userId.Value) >= 3)
            return BadRequest(new { message = "You can register up to three vehicles." });

        if (await _db.ResidentVehicles.AnyAsync(v => v.UserId == userId.Value && v.RegistrationNumber == plate))
            return BadRequest(new { message = "That vehicle is already registered." });

        var make = string.IsNullOrWhiteSpace(request.MakeAndModel) ? null : request.MakeAndModel.Trim();
        if (make != null && make.Length > 60)
            return BadRequest(new { message = "Make and model must be 60 characters or fewer." });

        var vehicle = new ResidentVehicle
        {
            UserId = userId.Value,
            RegistrationNumber = plate,
            MakeAndModel = make,
            RegisteredAt = DateTime.UtcNow
        };
        _db.ResidentVehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        return Ok(new VehicleDto
        {
            VehicleId = vehicle.VehicleId,
            RegistrationNumber = vehicle.RegistrationNumber,
            MakeAndModel = vehicle.MakeAndModel,
            RegisteredAt = vehicle.RegisteredAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var vehicle = await _db.ResidentVehicles
            .FirstOrDefaultAsync(v => v.VehicleId == id && v.UserId == userId.Value);
        if (vehicle == null) return NotFound(new { message = "Vehicle not found." });

        _db.ResidentVehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vehicle removed." });
    }
}
