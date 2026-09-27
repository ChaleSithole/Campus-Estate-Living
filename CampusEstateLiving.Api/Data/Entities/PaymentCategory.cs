using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Api.Data.Entities;

public class PaymentCategory
{
    [Key]
    public int CategoryId { get; set; }

    [Required, StringLength(60)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<Tariff> Tariffs { get; set; } =
        new List<Tariff>();

    public ICollection<Payment> Payments { get; set; } =
        new List<Payment>();
}