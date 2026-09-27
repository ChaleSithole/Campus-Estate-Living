namespace CampusEstateLiving.Shared.DTOs.Users;

public class UserSummaryDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool HasDisability { get; set; }
    public string? DisabilityDetails { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = [];
    public string? AccountNumber { get; set; }
    public string? AccountStatus { get; set; }
}

public class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string UserNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool HasDisability { get; set; }
    public string? DisabilityDetails { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool CreateEstateAccount { get; set; } = true;
}

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool HasDisability { get; set; }
    public string? DisabilityDetails { get; set; }
    public bool IsActive { get; set; }
    public string? AccountStatus { get; set; }
}

public class AuthoriseOfficerRequest
{
    public bool IsActive { get; set; } = true;
}
