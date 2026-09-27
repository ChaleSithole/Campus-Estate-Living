using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;

namespace CampusEstateLiving.Api.Services;

public class NotificationService
{
    private readonly CampusEstateDbContext _db;

    public NotificationService(CampusEstateDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAsync(
        int userId,
        string title,
        string message,
        string type = "Info",
        int? paymentId = null)
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
