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
    private readonly NotificationService _notify;

    public RoomAssignmentsController(CampusEstateDbContext context, NotificationService notify)
    {
        _context = context;
        _notify = notify;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<IEnumerable<RoomAssignmentDto>>> GetAll()
    {
        var assignments = await AssignmentQuery()
            .OrderByDescending(a => a.MoveOutDate == null)
            .ThenBy(a => a.Room.Property.BuildingName)
            .ThenBy(a => a.Room.RoomNumber)
            .ThenBy(a => a.MoveInDate)
            .ToListAsync();
        return Ok(assignments.Select(ToDto));
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

    [HttpPost("{id:int}/end")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> End(int id)
    {
        var assignment = await _context.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(a => a.AssignmentId == id);
        if (assignment == null) return NotFound(new { message = "Room assignment not found." });
        if (assignment.MoveOutDate != null)
            return BadRequest(new { message = "This assignment has already been closed." });

        var moveOutDate = DateTime.UtcNow.Date;
        if (moveOutDate < assignment.MoveInDate.Date)
            return BadRequest(new { message = "The assignment cannot be closed because the move-out date would be earlier than the move-in date." });

        assignment.MoveOutDate = moveOutDate;
        await _context.SaveChangesAsync();
        await _notify.NotifyAsync(
            assignment.UserId,
            "Accommodation assignment ended",
            $"Your accommodation assignment for {assignment.Room.Property.BuildingName} room {assignment.Room.RoomNumber} ended on {moveOutDate:dd MMM yyyy}.",
            "Accommodation");
        return Ok(new { message = "Assignment ended." });
    }

    private IQueryable<CampusEstateLiving.Api.Data.Entities.RoomAssignment> AssignmentQuery()
        => _context.RoomAssignments
            .AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u.Person)
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .Include(a => a.Room).ThenInclude(r => r.RoomType);

    private static RoomAssignmentDto ToDto(CampusEstateLiving.Api.Data.Entities.RoomAssignment a)
        => new()
        {
            AssignmentId = a.AssignmentId,
            UserId = a.UserId,
            UserNumber = a.User.UserNumber,
            StudentName = a.User.Person.FirstName + " " + a.User.Person.LastName,
            RoomId = a.RoomId,
            BuildingName = a.Room.Property.BuildingName,
            RoomNumber = a.Room.RoomNumber,
            RoomType = a.Room.RoomType.RoomTypeName,
            MoveInDate = a.MoveInDate,
            MoveOutDate = a.MoveOutDate,
            IsActive = a.MoveOutDate == null
        };
}