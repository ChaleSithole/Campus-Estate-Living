using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Api.Data.Entities;

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    public int? PaymentId { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment? Payment { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string NotificationType { get; set; } = "Info";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}