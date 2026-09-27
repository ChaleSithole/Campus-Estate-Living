using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationBadgeViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return View(0);
        var user = await _users.GetUserAsync(HttpContext.User);
        if (user == null) return View(0);
        var count = await _db.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
        return View(count);
    }
}
