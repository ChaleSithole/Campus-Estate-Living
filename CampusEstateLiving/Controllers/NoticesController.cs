using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize]
public class NoticesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NoticesController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var query = _db.Announcements
            .Include(a => a.CreatedByUser).ThenInclude(u => u.Person)
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > now)
            .AsQueryable();

        if (User.IsInRole(AppRoles.Student) || User.IsInRole(AppRoles.Staff))
            query = query.Where(a => a.Audience == AnnouncementAudiences.All || a.Audience == AnnouncementAudiences.Residents);
        else if (User.IsInRole(AppRoles.FinancialOfficer) && !User.IsInRole(AppRoles.Administrator))
            query = query.Where(a => a.Audience == AnnouncementAudiences.All || a.Audience == AnnouncementAudiences.Officers);

        var list = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .ToListAsync();
        return View(list);
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet]
    public IActionResult Create() => View(new AnnouncementCreateViewModel());

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
        _db.Announcements.Add(new Announcement
        {
            Title = model.Title.Trim(),
            Body = model.Body.Trim(),
            Audience = model.Audience,
            IsPinned = model.IsPinned,
            PublishedAt = DateTime.UtcNow,
            ExpiresAt = model.ExpiresAt,
            CreatedByUserId = user.Id
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notice published.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null) return NotFound();
        _db.Announcements.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notice removed.";
        return RedirectToAction(nameof(Index));
    }
}
