namespace CampusEstateLiving.Shared.DTOs.Accommodation;

public class CreatePropertyRequest
{
    public string BuildingCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PropertyStatus { get; set; } = "Active";
}

public class CreateRoomRequest
{
    public int PropertyId { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class RoomTypeDto
{
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
