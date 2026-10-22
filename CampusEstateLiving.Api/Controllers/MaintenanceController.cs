using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly CampusEstateDbContext _db;
    private readonly NotificationService _notify;

    public MaintenanceController(CampusEstateDbContext db, NotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaintenanceDto>>> Get(string? status = null)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var query = Query();
        if (!IsStaffSide())
            query = query.Where(r => r.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var items = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(items.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceDto>> GetOne(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var request = await Query().FirstOrDefaultAsync(r => r.RequestId == id);
        if (request == null) return NotFound(new { message = "Maintenance request not found." });
        if (!IsStaffSide() && request.UserId != userId.Value) return Forbid();
        return Ok(ToDto(request));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    public async Task<ActionResult<MaintenanceDto>> Create(CreateMaintenanceRequest model)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        if (!MaintenanceCategories.All.Contains(model.Category))
            return BadRequest(new { message = "Choose a valid maintenance category." });
        if (!MaintenancePriorities.All.Contains(model.Priority))
            return BadRequest(new { message = "Choose a valid priority." });
        if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Trim().Length > 120)
            return BadRequest(new { message = "Title is required and must be 120 characters or fewer." });
        if (string.IsNullOrWhiteSpace(model.Description) || model.Description.Trim().Length > 2000)
            return BadRequest(new { message = "Description is required and must be 2000 characters or fewer." });

        var roomId = await _db.RoomAssignments
            .Where(a => a.UserId == userId.Value && a.MoveOutDate == null)
            .Select(a => (int?)a.RoomId)
            .FirstOrDefaultAsync();

        var request = new MaintenanceRequest
        {
            UserId = userId.Value,
            RoomId = roomId,
            Category = model.Category,
            Priority = model.Priority,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Status = MaintenanceStatuses.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.MaintenanceRequests.Add(request);
        await _db.SaveChangesAsync();

        await _notify.NotifyAsync(userId.Value, "Maintenance logged",
            $"Your {model.Category.ToLower()} request \"{request.Title}\" has been logged. The estate office will update you.",
            "Maintenance");

        var created = await Query().FirstAsync(r => r.RequestId == request.RequestId);
        return Ok(ToDto(created));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    public async Task<ActionResult<MaintenanceDto>> Update(int id, UpdateMaintenanceRequest model)
    {
        if (!MaintenanceStatuses.All.Contains(model.Status))
            return BadRequest(new { message = "Choose a valid status." });
        if (!MaintenancePriorities.All.Contains(model.Priority))
            return BadRequest(new { message = "Choose a valid priority." });
        if (model.StaffNotes != null && model.StaffNotes.Trim().Length > 1000)
            return BadRequest(new { message = "Staff notes must be 1000 characters or fewer." });

        var request = await _db.MaintenanceRequests.FirstOrDefaultAsync(r => r.RequestId == id);
        if (request == null) return NotFound(new { message = "Maintenance request not found." });

        request.Status = model.Status;
        request.Priority = model.Priority;
        request.StaffNotes = string.IsNullOrWhiteSpace(model.StaffNotes) ? null : model.StaffNotes.Trim();
        request.UpdatedAt = DateTime.UtcNow;
        if (model.Status is MaintenanceStatuses.Resolved or MaintenanceStatuses.Closed)
            request.ResolvedAt ??= DateTime.UtcNow;
        else
            request.ResolvedAt = null;

        await _db.SaveChangesAsync();
        await _notify.NotifyAsync(request.UserId, "Maintenance update",
            $"Your request \"{request.Title}\" is now {request.Status.ToLower()}.",
            "Maintenance");

        var updated = await Query().FirstAsync(r => r.RequestId == id);
        return Ok(ToDto(updated));
    }

    private bool IsStaffSide()
        => User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.FinancialOfficer);

    private IQueryable<MaintenanceRequest> Query()
        => _db.MaintenanceRequests
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Person)
            .Include(r => r.Room).ThenInclude(room => room!.Property);

    private static MaintenanceDto ToDto(MaintenanceRequest request)
        => new()
        {
            RequestId = request.RequestId,
            UserId = request.UserId,
            ResidentName = request.User.Person.FullName,
            RoomId = request.RoomId,
            BuildingName = request.Room?.Property.BuildingName,
            RoomNumber = request.Room?.RoomNumber,
            Category = request.Category,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            ResolvedAt = request.ResolvedAt,
            StaffNotes = request.StaffNotes
        };
}
