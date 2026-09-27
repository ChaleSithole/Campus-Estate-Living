namespace CampusEstateLiving.Shared.DTOs.Accommodation;

public class PropertyDto
{
    public int PropertyId { get; set; }

    public string BuildingCode { get; set; } = string.Empty;

    public string BuildingName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public string PropertyStatus { get; set; } = string.Empty;

    public int RoomCount { get; set; }
}
