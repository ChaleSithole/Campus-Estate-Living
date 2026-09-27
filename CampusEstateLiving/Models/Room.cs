using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class Room
{
    [Key]
    public int RoomId { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [ForeignKey(nameof(PropertyId))]
    public Property Property { get; set; } = null!;

    [Required]
    public int RoomTypeId { get; set; }

    [ForeignKey(nameof(RoomTypeId))]
    public RoomType RoomType { get; set; } = null!;

    [Required, StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Notes { get; set; }

    [StringLength(20)]
    public string? SharingGender { get; set; }

    public ICollection<RoomAssignment> Assignments { get; set; } = new List<RoomAssignment>();
}
