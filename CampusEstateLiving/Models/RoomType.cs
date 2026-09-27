using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Models;

public class RoomType
{
    [Key]
    public int RoomTypeId { get; set; }

    [Required, StringLength(40)]
    public string RoomTypeName { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Capacity { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
