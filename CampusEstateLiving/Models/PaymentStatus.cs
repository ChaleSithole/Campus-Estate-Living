using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.Models;

public class PaymentStatus
{
    [Key]
    public int PaymentStatusId { get; set; }

    [Required, StringLength(40)]
    public string StatusName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
