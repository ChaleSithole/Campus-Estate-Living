using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(_users.GetUserId(User)!);
        var items = await _db.Notifications
            .Include(n => n.Payment)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = int.Parse(_users.GetUserId(User)!);
        var item = await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);
        if (item != null)
        {
            item.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = int.Parse(_users.GetUserId(User)!);
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }
}
