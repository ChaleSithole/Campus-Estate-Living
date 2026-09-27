using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Api.Data.Entities;

public class EstateAccount
{
    [Key]
    public int AccountId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string AccountStatus { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } =
        new List<Payment>();
}