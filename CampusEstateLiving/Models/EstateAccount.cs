using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

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
    public string AccountStatus { get; set; } = AccountStatuses.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
