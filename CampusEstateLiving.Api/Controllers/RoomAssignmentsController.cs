using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Accommodation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomAssignmentsController : ControllerBase
{
    private readonly CampusEstateDbContext _context;

    public RoomAssignmentsController(CampusEstateDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<RoomAssignmentDto>>> GetMyAssignments()
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var assignments = await _context.RoomAssignments
            .AsNoTracking()
            .Include(a => a.User)
                .ThenInclude(u => u.Person)
            .Include(a => a.Room)
                .ThenInclude(r => r.Property)
            .Include(a => a.Room)
                .ThenInclude(r => r.RoomType)
            .Where(a => a.UserId == userId.Value)
            .OrderByDescending(a => a.MoveInDate)
            .Select(a => new RoomAssignmentDto
            {
                AssignmentId = a.AssignmentId,
                UserId = a.UserId,
                UserNumber = a.User.UserNumber,
                StudentName = a.User.Person.FirstName + " " +
                              a.User.Person.LastName,
                RoomId = a.RoomId,
                BuildingName = a.Room.Property.BuildingName,
                RoomNumber = a.Room.RoomNumber,
                RoomType = a.Room.RoomType.RoomTypeName,
                MoveInDate = a.MoveInDate,
                MoveOutDate = a.MoveOutDate,
                IsActive = a.MoveOutDate == null
            })
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomAssignmentDto>> GetAssignment(int id)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var isStaff = User.IsInRole(AppRoles.Administrator) ||
                      User.IsInRole(AppRoles.FinancialOfficer);

        var assignment = await _context.RoomAssignments
            .AsNoTracking()
            .Include(a => a.User)
                .ThenInclude(u => u.Person)
            .Include(a => a.Room)
                .ThenInclude(r => r.Property)
            .Include(a => a.Room)
                .ThenInclude(r => r.RoomType)
            .Where(a =>
                a.AssignmentId == id &&
                (isStaff || a.UserId == userId.Value))
            .Select(a => new RoomAssignmentDto
            {
                AssignmentId = a.AssignmentId,
                UserId = a.UserId,
                UserNumber = a.User.UserNumber,
                StudentName = a.User.Person.FirstName + " " +
                              a.User.Person.LastName,
                RoomId = a.RoomId,
                BuildingName = a.Room.Property.BuildingName,
                RoomNumber = a.Room.RoomNumber,
                RoomType = a.Room.RoomType.RoomTypeName,
                MoveInDate = a.MoveInDate,
                MoveOutDate = a.MoveOutDate,
                IsActive = a.MoveOutDate == null
            })
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            return NotFound(new
            {
                message = "Room assignment not found."
            });
        }

        return Ok(assignment);
    }
}