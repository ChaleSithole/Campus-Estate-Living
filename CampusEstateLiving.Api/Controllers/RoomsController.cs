using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Shared.DTOs.Accommodation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusEstateLiving.Shared;
using Microsoft.AspNetCore.Identity;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly CampusEstateDbContext _context;
    private readonly UserManager<CampusEstateLiving.Api.Data.Entities.ApplicationUser> _users;

    public RoomsController(CampusEstateDbContext context, UserManager<CampusEstateLiving.Api.Data.Entities.ApplicationUser> users)
    {
        _context = context;
        _users = users;
    }

    [HttpPost("{roomId:int}/assign/{userId:int}")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> Assign(int roomId, int userId, [FromQuery] DateTime? moveInDate)
    {
        var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomId == roomId);
        var resident = await _users.Users.Include(u => u.Person).FirstOrDefaultAsync(u => u.Id == userId);
        if (room == null || resident == null) return NotFound(new { message = "Room or resident not found." });
        if (!await _users.IsInRoleAsync(resident, AppRoles.Student) && !await _users.IsInRoleAsync(resident, AppRoles.Staff)) return BadRequest(new { message = "Only residents can be assigned rooms." });
        if (await _context.RoomAssignments.AnyAsync(a => a.UserId == userId && a.MoveOutDate == null)) return BadRequest(new { message = "Resident already has an active room assignment." });
        var occupants = await _context.RoomAssignments.Include(a => a.User).ThenInclude(u => u.Person).Where(a => a.RoomId == roomId && a.MoveOutDate == null).ToListAsync();
        if (occupants.Count >= room.RoomType.Capacity) return BadRequest(new { message = "Room is at capacity." });
        if (room.RoomType.RoomTypeName.Equals(RoomTypeNames.Sharing, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(resident.Person.Gender)) return BadRequest(new { message = "Resident gender is required for sharing-room assignment." });
            var established = room.SharingGender ?? occupants.FirstOrDefault()?.User.Person.Gender;
            if (!string.IsNullOrWhiteSpace(established) && !established.Equals(resident.Person.Gender, StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "This room already has a resident of a different gender and cannot be shared." });
            room.SharingGender ??= resident.Person.Gender;
        }
        _context.RoomAssignments.Add(new CampusEstateLiving.Api.Data.Entities.RoomAssignment { RoomId = roomId, UserId = userId, MoveInDate = moveInDate?.Date ?? DateTime.UtcNow.Date });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Resident assigned." });
    }

    [HttpGet("types")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<IEnumerable<RoomTypeDto>>> Types()
    {
        var types = await _context.RoomTypes
            .AsNoTracking()
            .OrderBy(t => t.RoomTypeName)
            .Select(t => new RoomTypeDto
            {
                RoomTypeId = t.RoomTypeId,
                RoomTypeName = t.RoomTypeName,
                Capacity = t.Capacity
            })
            .ToListAsync();
        return Ok(types);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomRequest request)
    {
        var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyId == request.PropertyId);
        var limits = property?.BuildingCode switch
        {
            "QWA-A" => (Total: 48, Single: 48, Sharing: 0),
            "QWA-B" or "QWA-C" or "QWA-D" => (Total: 72, Single: 54, Sharing: 18),
            _ => (Total: 0, Single: 0, Sharing: 0)
        };
        if (property == null || limits.Total == 0)
            return BadRequest(new { message = "Choose one of the four configured estate buildings." });

        var roomNumber = request.RoomNumber.Trim();
        if (roomNumber.Length is < 1 or > 20)
            return BadRequest(new { message = "Enter a room number of 1 to 20 characters." });
        if (await _context.Rooms.AnyAsync(r => r.PropertyId == request.PropertyId && r.RoomNumber == roomNumber))
            return Conflict(new { message = "That room number already exists in this building." });

        var selected = await _context.RoomTypes.FirstOrDefaultAsync(t => t.RoomTypeId == request.RoomTypeId);
        if (selected == null || selected.RoomTypeName is not (RoomTypeNames.Single or RoomTypeNames.Sharing))
            return BadRequest(new { message = "Choose Single or Sharing room type." });

        var existing = await _context.Rooms.Where(r => r.PropertyId == request.PropertyId).ToListAsync();
        var singleTypeId = await _context.RoomTypes.Where(t => t.RoomTypeName == RoomTypeNames.Single).Select(t => t.RoomTypeId).FirstOrDefaultAsync();
        var sharingTypeId = await _context.RoomTypes.Where(t => t.RoomTypeName == RoomTypeNames.Sharing).Select(t => t.RoomTypeId).FirstOrDefaultAsync();
        var singleCount = existing.Count(r => r.RoomTypeId == singleTypeId);
        var sharingCount = existing.Count(r => r.RoomTypeId == sharingTypeId);
        if (existing.Count >= limits.Total)
            return BadRequest(new { message = $"{property.BuildingName} already has its full {limits.Total}-room allocation." });
        if (selected.RoomTypeName == RoomTypeNames.Single && singleCount >= limits.Single)
            return BadRequest(new { message = "This building already has its full allocation of single rooms." });
        if (selected.RoomTypeName == RoomTypeNames.Sharing && sharingCount >= limits.Sharing)
            return BadRequest(new { message = "This building already has its full allocation of sharing rooms." });

        var room = new Room
        {
            PropertyId = request.PropertyId,
            RoomTypeId = request.RoomTypeId,
            RoomNumber = roomNumber,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return Ok(new RoomDto
        {
            RoomId = room.RoomId,
            PropertyId = room.PropertyId,
            BuildingName = property.BuildingName,
            RoomNumber = room.RoomNumber,
            RoomType = selected.RoomTypeName,
            Capacity = selected.Capacity,
            CurrentOccupants = 0,
            AvailableSpaces = selected.Capacity,
            Notes = room.Notes
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms()
    {
        var rooms = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .Include(r => r.Assignments)
            .OrderBy(r => r.Property.BuildingName)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();

        var roomDtos = rooms.Select(r => new RoomDto
        {
            RoomId = r.RoomId,
            PropertyId = r.PropertyId,
            BuildingName = r.Property.BuildingName,
            RoomNumber = r.RoomNumber,
            RoomType = r.RoomType.RoomTypeName,
            Capacity = r.RoomType.Capacity,
            CurrentOccupants = r.Assignments.Count(a => a.MoveOutDate == null),
            AvailableSpaces = Math.Max(
                0,
                r.RoomType.Capacity -
                r.Assignments.Count(a => a.MoveOutDate == null)),
            Notes = r.Notes
        }).ToList();

        return Ok(roomDtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetRoom(int id)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.RoomId == id);

        if (room == null)
        {
            return NotFound(new
            {
                message = "Room not found."
            });
        }

        var activeOccupants = room.Assignments
            .Count(a => a.MoveOutDate == null);

        var roomDto = new RoomDto
        {
            RoomId = room.RoomId,
            PropertyId = room.PropertyId,
            BuildingName = room.Property.BuildingName,
            RoomNumber = room.RoomNumber,
            RoomType = room.RoomType.RoomTypeName,
            Capacity = room.RoomType.Capacity,
            CurrentOccupants = activeOccupants,
            AvailableSpaces = Math.Max(
                0,
                room.RoomType.Capacity - activeOccupants),
            Notes = room.Notes
        };

        return Ok(roomDto);
    }

    [HttpGet("property/{propertyId:int}")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRoomsByProperty(
        int propertyId)
    {
        var propertyExists = await _context.Properties
            .AnyAsync(p => p.PropertyId == propertyId);

        if (!propertyExists)
        {
            return NotFound(new
            {
                message = "Property not found."
            });
        }

        var rooms = await _context.Rooms
            .AsNoTracking()
            .Where(r => r.PropertyId == propertyId)
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .Include(r => r.Assignments)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        var roomDtos = rooms.Select(r =>
        {
            var activeOccupants = r.Assignments
                .Count(a => a.MoveOutDate == null);

            return new RoomDto
            {
                RoomId = r.RoomId,
                PropertyId = r.PropertyId,
                BuildingName = r.Property.BuildingName,
                RoomNumber = r.RoomNumber,
                RoomType = r.RoomType.RoomTypeName,
                Capacity = r.RoomType.Capacity,
                CurrentOccupants = activeOccupants,
                AvailableSpaces = Math.Max(
                    0,
                    r.RoomType.Capacity - activeOccupants),
                Notes = r.Notes
            };
        }).ToList();

        return Ok(roomDtos);
    }
}
