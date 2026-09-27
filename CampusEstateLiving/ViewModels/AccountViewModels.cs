using System.ComponentModel.DataAnnotations;

namespace CampusEstateLiving.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(80)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "I am a")]
    public string AccountType { get; set; } = "Student";

    [Required, StringLength(40)]
    [Display(Name = "Student / staff number")]
    public string UserNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    [Display(Name = "I have a disability")]
    public bool HasDisability { get; set; }

    [StringLength(500)]
    [Display(Name = "Disability details (optional)")]
    public string? DisabilityDetails { get; set; }

    [Phone, StringLength(20)]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetUserPasswordViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
