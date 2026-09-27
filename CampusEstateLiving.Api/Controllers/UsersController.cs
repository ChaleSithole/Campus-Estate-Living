using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Auth;
using CampusEstateLiving.Shared.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
public class UsersController : ControllerBase
{
    private readonly CampusEstateDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly EstateAccountService _accounts;
    private readonly NotificationService _notify;

    public UsersController(
        CampusEstateDbContext db,
        UserManager<ApplicationUser> users,
        EstateAccountService accounts,
        NotificationService notify)
    {
        _db = db;
        _users = users;
        _accounts = accounts;
        _notify = notify;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> Get(
        string? role = null,
        string? q = null)
    {
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (User.IsInRole(AppRoles.FinancialOfficer) &&
            !User.IsInRole(AppRoles.Administrator))
        {
            query = query.Where(u => u.EstateAccount != null);
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.UserRoles.Any(r => r.Role.Name == role));

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u =>
                u.Email!.Contains(q) ||
                u.UserNumber.Contains(q) ||
                u.Person.FirstName.Contains(q) ||
                u.Person.LastName.Contains(q));
        }

        var users = await query
            .OrderBy(u => u.Person.LastName)
            .ThenBy(u => u.Person.FirstName)
            .ToListAsync();

        return Ok(users.Select(ToSummary));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserSummaryDto>> GetById(int id)
    {
        var user = await LoadUser(id);
        if (user == null) return NotFound(new { message = "User not found." });
        return Ok(ToSummary(user));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<UserSummaryDto>> Create(CreateUserRequest request)
    {
        if (!AppRoles.All.Contains(request.Role))
        {
            return BadRequest(new { message = "Invalid role." });
        }

        var expectedLength = request.Role switch
        {
            AppRoles.Student => 10,
            AppRoles.Staff or AppRoles.FinancialOfficer or AppRoles.Administrator => 7,
            _ => 0
        };
        if (request.UserNumber.Length != expectedLength || request.UserNumber.Any(c => !char.IsAsciiDigit(c)))
            return BadRequest(new { message = $"{(request.Role == AppRoles.FinancialOfficer ? "Officer" : request.Role == AppRoles.Administrator ? "Admin" : request.Role)} number must contain exactly {expectedLength} digits." });

        if (await _users.FindByEmailAsync(request.Email.Trim()) != null)
        {
            return Conflict(new { message = "Email already in use." });
        }

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
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
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
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });
        }

        await _users.AddToRoleAsync(user, request.Role);

        if (request.CreateEstateAccount && AppRoles.Resident.Contains(request.Role))
        {
            await _accounts.EnsureForResidentAsync(user);
        }

        await _notify.NotifyAsync(
            user.Id,
            "Account created",
            $"An administrator created your {request.Role} account.",
            "Account");

        var loaded = await LoadUser(user.Id);
        return Ok(ToSummary(loaded!));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound(new { message = "User not found." });

        user.Person.FirstName = request.FirstName.Trim();
        user.Person.LastName = request.LastName.Trim();
        user.Person.PhoneNumber = request.PhoneNumber;
        user.Person.DateOfBirth = request.DateOfBirth;
        user.Person.Gender = request.Gender;
        user.Person.HasDisability = request.HasDisability;
        user.Person.DisabilityDetails = request.HasDisability ? request.DisabilityDetails : null;
        user.PhoneNumber = request.PhoneNumber;
        var roles = await _users.GetRolesAsync(user);
        var expectedLength = roles.Contains(AppRoles.Student) ? 10 : 7;
        if (request.UserNumber.Length != expectedLength || request.UserNumber.Any(c => !char.IsAsciiDigit(c)))
            return BadRequest(new { message = $"Identification number must contain exactly {expectedLength} digits." });
        user.UserNumber = request.UserNumber.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (user.EstateAccount != null && !string.IsNullOrWhiteSpace(request.AccountStatus))
        {
            user.EstateAccount.AccountStatus = request.AccountStatus;
            user.EstateAccount.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "User updated." });
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deactivated." });
    }

    [HttpPost("{id:int}/authorise")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> Authorise(int id, AuthoriseOfficerRequest request)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "User not found." });

        var roles = await _users.GetRolesAsync(user);
        if (!roles.Contains(AppRoles.FinancialOfficer))
        {
            return BadRequest(new { message = "Only financial officer accounts can be authorised here." });
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        await _notify.NotifyAsync(
            user.Id,
            request.IsActive ? "Officer authorised" : "Officer access revoked",
            request.IsActive
                ? "An administrator authorised you to perform financial officer duties."
                : "An administrator revoked financial officer access on this account.",
            "Account");

        return Ok(new { message = request.IsActive ? "Financial officer authorised." : "Financial officer deactivated." });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "User not found." });

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await _notify.NotifyAsync(
            user.Id,
            "Password reset",
            "A financial officer or administrator reset the password on this account.",
            "Account");

        return Ok(new { message = "Password reset." });
    }

    private async Task<ApplicationUser?> LoadUser(int id)
        => await _db.Users
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

    private static UserSummaryDto ToSummary(ApplicationUser user)
        => new()
        {
            UserId = user.Id,
            FullName = user.Person.FullName,
            Email = user.Email ?? string.Empty,
            UserNumber = user.UserNumber,
            DateOfBirth = user.Person.DateOfBirth,
            Gender = user.Person.Gender,
            HasDisability = user.Person.HasDisability,
            DisabilityDetails = user.Person.DisabilityDetails,
            IsActive = user.IsActive,
            Roles = user.UserRoles
                .Where(ur => ur.Role.Name != null)
                .Select(ur => ur.Role.Name!)
                .ToList(),
            AccountNumber = user.EstateAccount?.AccountNumber,
            AccountStatus = user.EstateAccount?.AccountStatus
        };
}
