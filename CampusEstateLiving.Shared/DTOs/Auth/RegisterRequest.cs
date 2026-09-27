namespace CampusEstateLiving.Shared.DTOs.Auth;

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Student";
    public string UserNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool HasDisability { get; set; }
    public string? DisabilityDetails { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
