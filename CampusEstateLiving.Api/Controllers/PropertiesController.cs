using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Accommodation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly CampusEstateDbContext _context;

    public PropertiesController(CampusEstateDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropertyDto>>> GetProperties()
    {
        var properties = await _context.Properties
            .AsNoTracking()
            .OrderBy(p => p.BuildingName)
            .Select(p => new PropertyDto
            {
                PropertyId = p.PropertyId,
                BuildingCode = p.BuildingCode,
                BuildingName = p.BuildingName,
                Description = p.Description,
                Address = p.Address,
                PropertyStatus = p.PropertyStatus,
                RoomCount = p.Rooms.Count
            })
            .ToListAsync();

        return Ok(properties);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyDto>> GetProperty(int id)
    {
        var property = await _context.Properties
            .AsNoTracking()
            .Where(p => p.PropertyId == id)
            .Select(p => new PropertyDto
            {
                PropertyId = p.PropertyId,
                BuildingCode = p.BuildingCode,
                BuildingName = p.BuildingName,
                Description = p.Description,
                Address = p.Address,
                PropertyStatus = p.PropertyStatus,
                RoomCount = p.Rooms.Count
            })
            .FirstOrDefaultAsync();

        if (property == null)
        {
            return NotFound(new
            {
                message = "Property not found."
            });
        }

        return Ok(property);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<PropertyDto>> Create(CreatePropertyRequest request)
    {
        if (await _context.Properties.CountAsync() >= 4)
            return BadRequest(new { message = "The estate is configured for four buildings." });

        var code = request.BuildingCode.Trim();
        var name = request.BuildingName.Trim();
        var address = request.Address.Trim();
        var status = string.IsNullOrWhiteSpace(request.PropertyStatus) ? "Active" : request.PropertyStatus.Trim();
        if (code.Length is < 1 or > 40 || name.Length is < 1 or > 120 || address.Length is < 1 or > 250)
            return BadRequest(new { message = "Enter a building code, name and address within the allowed lengths." });
        if (status is not ("Active" or "Inactive"))
            return BadRequest(new { message = "Property status must be Active or Inactive." });
        if (await _context.Properties.AnyAsync(p => p.BuildingCode == code))
            return Conflict(new { message = "That building code is already in use." });

        var property = new Property
        {
            BuildingCode = code,
            BuildingName = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Address = address,
            PropertyStatus = status,
            CreatedAt = DateTime.UtcNow
        };
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return Ok(new PropertyDto
        {
            PropertyId = property.PropertyId,
            BuildingCode = property.BuildingCode,
            BuildingName = property.BuildingName,
            Description = property.Description,
            Address = property.Address,
            PropertyStatus = property.PropertyStatus,
            RoomCount = 0
        });
    }
}