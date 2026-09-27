using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Models;

public class Property
{
    [Key]
    public int PropertyId { get; set; }

    [Required, StringLength(40)]
    public string BuildingCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string BuildingName { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string PropertyStatus { get; set; } = PropertyStatuses.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
