using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared.DTOs.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public NotificationsController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> Get()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var items = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                NotificationType = n.NotificationType,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                PaymentId = n.PaymentId
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var count = await _db.Notifications.CountAsync(n =>
            n.UserId == userId.Value && !n.IsRead);

        return Ok(new { count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

        if (notification == null)
            return NotFound(new { message = "Notification not found." });

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Notification marked as read." });
    }
}
