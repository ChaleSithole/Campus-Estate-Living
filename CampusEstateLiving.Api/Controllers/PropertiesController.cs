using CampusEstateLiving.Api.Data;
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
}