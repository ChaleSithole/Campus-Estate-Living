using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Api.Data.Entities;

public class Tariff
{
    [Key]
    public int TariffId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public PaymentCategory Category { get; set; } = null!;

    [Required, StringLength(80)]
    public string TariffName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Required, StringLength(40)]
    public string Unit { get; set; } = "Monthly";

    public bool IsActive { get; set; }
}