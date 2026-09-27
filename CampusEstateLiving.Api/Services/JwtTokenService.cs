using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusEstateLiving.Api.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CampusEstateLiving.Api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(
        ApplicationUser user,
        IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("UserNumber", user.UserNumber)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is not configured.");

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt
        );
    }
}
