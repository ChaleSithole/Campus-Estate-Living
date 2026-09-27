using System.ComponentModel.DataAnnotations;
using CampusEstateLiving.Models;

namespace CampusEstateLiving.ViewModels;

public class StatementViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public List<Payment> Payments { get; set; } = [];
    public decimal ConfirmedTotal { get; set; }
    public decimal PendingTotal { get; set; }
    public string PeriodLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
}

public class AnnouncementCreateViewModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = AnnouncementAudiences.All;

    public bool IsPinned { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class MaintenanceCreateViewModel
{
    [Required]
    public string Category { get; set; } = MaintenanceCategories.General;

    [Required]
    public string Priority { get; set; } = MaintenancePriorities.Normal;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;
}

public class MaintenanceUpdateViewModel
{
    public int RequestId { get; set; }

    [Required]
    public string Status { get; set; } = MaintenanceStatuses.Open;

    [Required]
    public string Priority { get; set; } = MaintenancePriorities.Normal;

    [StringLength(1000)]
    public string? StaffNotes { get; set; }
}
