using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class ResidentVehicle
{
    [Key]
    public int VehicleId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(12)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [StringLength(60)]
    public string? MakeAndModel { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
