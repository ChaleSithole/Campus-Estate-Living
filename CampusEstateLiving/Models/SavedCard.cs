using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class SavedCard
{
    [Key]
    public int CardId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(80)]
    public string CardholderName { get; set; } = string.Empty;

    [Required, StringLength(4)]
    public string Last4 { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Brand { get; set; } = "Card";

    [Range(1, 12)]
    public int ExpiryMonth { get; set; }

    [Range(2024, 2100)]
    public int ExpiryYear { get; set; }

    /// <summary>Payment-provider token; never a locally generated representation of card details.</summary>
    [Required, StringLength(128)]
    public string Token { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
