using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Api.Data.Entities;

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

    public int ExpiryMonth { get; set; }

    public int ExpiryYear { get; set; }

    [Required, StringLength(128)]
    public string Token { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }
}