using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusEstateLiving.Api.Data.Entities;

public class BankAccount
{
    [Key]
    public int BankAccountId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(80)]
    public string BankName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string AccountHolder { get; set; } = string.Empty;

    [Required, StringLength(4)]
    public string AccountNumberLast4 { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string BranchCode { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string AccountType { get; set; } = "Cheque";

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }
}