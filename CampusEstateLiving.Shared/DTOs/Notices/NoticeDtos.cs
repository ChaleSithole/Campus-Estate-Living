namespace CampusEstateLiving.Shared.DTOs.Notices;

/// <summary>
/// Matches the Announcement entity and NoticesController.
/// </summary>
public class AnnouncementDto
{
    public int AnnouncementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Audience { get; set; } = "All";
    public bool IsPinned { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
