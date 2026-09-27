namespace CampusEstateLiving.Shared.DTOs.Accommodation;

public class RoomAssignmentDto
{
    public int AssignmentId { get; set; }
    public int UserId { get; set; }
    public string UserNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
    public bool IsActive { get; set; }
}
