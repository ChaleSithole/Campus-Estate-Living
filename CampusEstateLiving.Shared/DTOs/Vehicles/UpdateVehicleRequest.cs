using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Shared.DTOs.Vehicles;

public class UpdateVehicleRequest
{
    [Required]
    [StringLength(20)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [StringLength(30)]
    public string Colour { get; set; } = string.Empty;

    public string? Status { get; set; }
}