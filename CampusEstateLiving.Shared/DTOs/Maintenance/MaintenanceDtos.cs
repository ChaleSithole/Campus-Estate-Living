namespace CampusEstateLiving.Shared.DTOs.Maintenance;

public class MaintenanceDto
{
    public int RequestId { get; set; }
    public int Id { get => RequestId; set => RequestId = value; }
    public int UserId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? StaffNotes { get; set; }
}

public class CreateMaintenanceRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Category { get; set; } = "General";
    public string Priority { get; set; } = "Normal";
    [System.ComponentModel.DataAnnotations.Required]
    public string Title { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string Description { get; set; } = string.Empty;
}

public class UpdateMaintenanceRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public string? StaffNotes { get; set; }
}
