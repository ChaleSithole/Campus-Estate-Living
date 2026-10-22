namespace CampusEstateLiving.Shared.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }

    public string UserNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    /// <summary>Identity surrogate keys. Not the product role ids 1–4.</summary>
    public List<int> IdentityRoleIds { get; set; } = new();

    /// <summary>Product role id: 1 Administrator, 2 Financial Officer, 3 Student, 4 Staff.</summary>
    public int PrimaryRoleId { get; set; }

    /// <summary>Administrator, FinancialOfficer, or Resident. Students and staff are Resident.</summary>
    public string UiCategory { get; set; } = "Resident";
}