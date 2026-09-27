using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Models;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public EstateAccount Account { get; set; } = null!;

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public PaymentCategory Category { get; set; } = null!;

    [Required]
    public int PaymentStatusId { get; set; }

    [ForeignKey(nameof(PaymentStatusId))]
    public PaymentStatus PaymentStatus { get; set; } = null!;

    public int? ProcessedByUserId { get; set; }

    [ForeignKey(nameof(ProcessedByUserId))]
    public ApplicationUser? ProcessedByUser { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [StringLength(500)]
    public string? ProcessingNotes { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(20)]
    public string PaymentMethod { get; set; } = PaymentMethods.Eft;

    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Notes { get; set; }

    public int? SavedCardId { get; set; }

    [ForeignKey(nameof(SavedCardId))]
    public SavedCard? SavedCard { get; set; }

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
