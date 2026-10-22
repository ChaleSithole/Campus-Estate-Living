using System;

namespace CampusEstateLiving.Shared.DTOs.Notices;

public class NoticeDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string? AuthorName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;
}