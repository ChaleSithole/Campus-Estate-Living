using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Api.Data.Entities;

public class StaffMessage
{
    public int MessageId { get; set; }
    public int SenderUserId { get; set; }
    public int RecipientUserId { get; set; }

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
