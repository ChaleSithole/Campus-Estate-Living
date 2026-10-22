namespace CampusEstateLiving.Shared.DTOs.Vehicles;

public class VehicleDto
{
    public int VehicleId { get; set; }
    public int Id { get => VehicleId; set => VehicleId = value; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? MakeAndModel { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RegisteredAt { get; set; }
}

public class AddVehicleRequest
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? MakeAndModel { get; set; }
}
