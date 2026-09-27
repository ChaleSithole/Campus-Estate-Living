using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Api.Data.Entities;

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
    public string PropertyStatus { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}