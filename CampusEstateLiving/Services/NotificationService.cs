using CampusEstateLiving.Data;
using CampusEstateLiving.Models;

namespace CampusEstateLiving.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAsync(int userId, string title, string message, string type = "Info", int? paymentId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = type,
            PaymentId = paymentId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
