using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Api.Data.Entities;

public class Review
{
    [Key]
    public int ReviewId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    public int Rating { get; set; }

    [Required, StringLength(800)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}