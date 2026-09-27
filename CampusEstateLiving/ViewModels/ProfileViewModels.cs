using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.ViewModels;

public class ProfileEditViewModel
{
    public int PersonId { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Student / employee number")]
    public string UserNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    public bool HasDisability { get; set; }

    [StringLength(500)]
    public string? DisabilityDetails { get; set; }
}

public class AddCardViewModel
{
    [Required, StringLength(80)]
    [Display(Name = "Name on card")]
    public string CardholderName { get; set; } = string.Empty;

    [Required, StringLength(19, MinimumLength = 13)]
    [RegularExpression("^[0-9 ]+$", ErrorMessage = "Enter a valid demo card number.")]
    [Display(Name = "Card number")]
    public string CardNumber { get; set; } = string.Empty;

    [Required, StringLength(4, MinimumLength = 3)]
    [RegularExpression("^[0-9]+$", ErrorMessage = "Enter a valid demo security code.")]
    [Display(Name = "Security code")]
    public string Cvv { get; set; } = string.Empty;

    [Range(1, 12)] public int ExpiryMonth { get; set; } = DateTime.UtcNow.Month;
    [Range(2026, 2100)] public int ExpiryYear { get; set; } = DateTime.UtcNow.Year + 2;

    public bool IsDefault { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class AddBankViewModel
{
    [Required, StringLength(80)]
    [Display(Name = "Bank")]
    public string BankName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Account holder")]
    public string AccountHolder { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Account number")]
    [RegularExpression(@"^[0-9]{6,16}$", ErrorMessage = "Enter a valid account number.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Branch code")]
    public string BranchCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Account type")]
    public string AccountType { get; set; } = "Cheque";

    public bool IsDefault { get; set; } = true;
}
