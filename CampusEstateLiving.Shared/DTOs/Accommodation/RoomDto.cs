namespace CampusEstateLiving.Shared.DTOs.Accommodation;

public class RoomDto
{
    public int RoomId { get; set; }

    public int PropertyId { get; set; }

    public string BuildingName { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int CurrentOccupants { get; set; }

    public int AvailableSpaces { get; set; }

    public string? Notes { get; set; }
}
