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
}