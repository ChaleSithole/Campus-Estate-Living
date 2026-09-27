using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CampusEstateDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenService _jwt;
    private readonly EstateAccountService _accounts;
    private readonly NotificationService _notify;

    public AuthController(
        CampusEstateDbContext db,
        UserManager<ApplicationUser> users,
        JwtTokenService jwt,
        EstateAccountService accounts,
        NotificationService notify)
    {
        _db = db;
        _users = users;
        _jwt = jwt;
        _accounts = accounts;
        _notify = notify;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _users.Users
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim());

        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (await _users.IsLockedOutAsync(user))
        {
            return Unauthorized(new { message = "This account is temporarily locked. Try again later." });
        }

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        await _users.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        var roles = (await _users.GetRolesAsync(user)).ToList();
        var (token, expiresAt) = _jwt.CreateToken(user, roles);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            UserNumber = user.UserNumber,
            Email = user.Email ?? string.Empty,
            FullName = user.Person.FullName,
            Roles = roles
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        if (request.AccountType is not (AppRoles.Student or AppRoles.Staff))
        {
            return BadRequest(new { message = "Public registration is only available for students and staff." });
        }

        var expectedNumberLength = request.AccountType == AppRoles.Student ? 10 : 7;
        if (request.UserNumber.Length != expectedNumberLength || request.UserNumber.Any(c => !char.IsAsciiDigit(c)))
            return BadRequest(new { message = $"{(request.AccountType == AppRoles.Student ? "Student" : "Staff")} number must contain exactly {expectedNumberLength} digits." });

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Password and confirmation do not match." });
        }

        var email = request.Email.Trim();
        if (await _users.FindByEmailAsync(email) != null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var person = new Person
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                HasDisability = request.HasDisability,
                DisabilityDetails = request.HasDisability ? request.DisabilityDetails : null,
                CreatedAt = DateTime.UtcNow
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PersonId = person.PersonId,
                UserNumber = request.UserNumber.Trim(),
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var created = await _users.CreateAsync(user, request.Password);
            if (!created.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { errors = created.Errors.Select(e => e.Description) });
            }

            var roleResult = await _users.AddToRoleAsync(user, request.AccountType);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { errors = roleResult.Errors.Select(e => e.Description) });
            }

            await _accounts.EnsureForResidentAsync(user);
            await _notify.NotifyAsync(
                user.Id,
                "Welcome to Campus Estate Living",
                "Your resident account is ready. Add a card or bank account, then pay rent, water, electricity, parking or refuse from Payments.",
                "Welcome");

            await transaction.CommitAsync();

            user.Person = person;
            var roles = (await _users.GetRolesAsync(user)).ToList();
            var (token, expiresAt) = _jwt.CreateToken(user, roles);

            return Ok(new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                UserNumber = user.UserNumber,
                Email = user.Email ?? string.Empty,
                FullName = person.FullName,
                Roles = roles
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "We could not create your account. Please try again." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<LoginResponse>> Me()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _users.Users
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null) return Unauthorized();

        var roles = (await _users.GetRolesAsync(user)).ToList();
        return Ok(new LoginResponse
        {
            UserId = user.Id,
            UserNumber = user.UserNumber,
            Email = user.Email ?? string.Empty,
            FullName = user.Person.FullName,
            Roles = roles
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Password and confirmation do not match." });
        }

        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _users.FindByIdAsync(userId.Value.ToString());
        if (user == null) return Unauthorized();

        var result = await _users.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        return Ok(new { message = "Password updated." });
    }
}
