using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly NotificationService _notify;

    public MaintenanceController(ApplicationDbContext db, UserManager<ApplicationUser> users, NotificationService notify)
    {
        _db = db;
        _users = users;
        _notify = notify;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var query = _db.MaintenanceRequests
            .Include(r => r.User).ThenInclude(u => u.Person)
            .Include(r => r.Room).ThenInclude(room => room!.Property)
            .AsQueryable();

        var isStaffSide = User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.FinancialOfficer);
        if (!isStaffSide)
        {
            var user = await _users.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
            query = query.Where(r => r.UserId == user.Id);
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        ViewBag.Status = status;
        ViewBag.IsStaffSide = isStaffSide;
        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpGet]
    public IActionResult Create() => View(new MaintenanceCreateViewModel());

    [Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        var assignment = await _db.RoomAssignments
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.MoveOutDate == null);

        var request = new MaintenanceRequest
        {
            UserId = user.Id,
            RoomId = assignment?.RoomId,
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

        await _notify.NotifyAsync(user.Id, "Maintenance logged",
            $"Your {model.Category.ToLower()} request “{request.Title}” has been logged. The estate office will update you.",
            "Maintenance");

        TempData["Success"] = "Maintenance request submitted.";
        return RedirectToAction(nameof(Details), new { id = request.RequestId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await _db.MaintenanceRequests
            .Include(r => r.User).ThenInclude(u => u.Person)
            .Include(r => r.Room).ThenInclude(room => room!.Property)
            .FirstOrDefaultAsync(r => r.RequestId == id);
        if (request == null) return NotFound();

        var isStaffSide = User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.FinancialOfficer);
        var user = await _users.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        if (!isStaffSide && request.UserId != user.Id) return Forbid();

        ViewBag.IsStaffSide = isStaffSide;
        ViewBag.Update = new MaintenanceUpdateViewModel
        {
            RequestId = request.RequestId,
            Status = request.Status,
            Priority = request.Priority,
            StaffNotes = request.StaffNotes
        };
        return View(request);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(MaintenanceUpdateViewModel model)
    {
        var request = await _db.MaintenanceRequests.FindAsync(model.RequestId);
        if (request == null) return NotFound();

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
            $"Your request “{request.Title}” is now {request.Status.ToLower()}.",
            "Maintenance");
        TempData["Success"] = "Request updated.";
        return RedirectToAction(nameof(Details), new { id = request.RequestId });
    }
}
