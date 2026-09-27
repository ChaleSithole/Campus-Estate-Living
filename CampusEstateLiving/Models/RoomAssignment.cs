using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class RoomAssignment
{
    [Key]
    public int AssignmentId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required]
    public int RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime MoveInDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? MoveOutDate { get; set; }

    [NotMapped]
    public bool IsActive => MoveOutDate == null;
}
