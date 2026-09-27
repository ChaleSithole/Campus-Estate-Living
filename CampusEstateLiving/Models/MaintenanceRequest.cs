using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class MaintenanceRequest
{
    [Key]
    public int RequestId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    public int? RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room? Room { get; set; }

    [Required, StringLength(40)]
    public string Category { get; set; } = MaintenanceCategories.General;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Status { get; set; } = MaintenanceStatuses.Open;

    [Required, StringLength(20)]
    public string Priority { get; set; } = MaintenancePriorities.Normal;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    [StringLength(1000)]
    public string? StaffNotes { get; set; }
}

public static class MaintenanceCategories
{
    public const string Plumbing = "Plumbing";
    public const string Electrical = "Electrical";
    public const string Furniture = "Furniture";
    public const string Security = "Security";
    public const string General = "General";

    public static readonly string[] All = [Plumbing, Electrical, Furniture, Security, General];
}

public static class MaintenanceStatuses
{
    public const string Open = "Open";
    public const string InProgress = "In progress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";

    public static readonly string[] All = [Open, InProgress, Resolved, Closed];
}

public static class MaintenancePriorities
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";

    public static readonly string[] All = [Low, Normal, High, Urgent];
}
