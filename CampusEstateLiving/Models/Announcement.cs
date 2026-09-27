using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Audience { get; set; } = AnnouncementAudiences.All;

    public bool IsPinned { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

public static class AnnouncementAudiences
{
    public const string All = "All";
    public const string Residents = "Residents";
    public const string Officers = "Officers";
}
