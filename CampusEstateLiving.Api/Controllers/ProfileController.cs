using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared.DTOs.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public ProfileController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.EstateAccount)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.RoomAssignments).ThenInclude(a => a.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null) return Unauthorized();

        var assignment = user.RoomAssignments.FirstOrDefault(a => a.MoveOutDate == null);

        return Ok(new ProfileDto
        {
            UserId = user.Id,
            PersonId = user.PersonId,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            FullName = user.Person.FullName,
            UserNumber = user.UserNumber,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.Person.PhoneNumber,
            IsActive = user.IsActive,
            Roles = user.UserRoles
                .Where(ur => ur.Role.Name != null)
                .Select(ur => ur.Role.Name!)
                .ToList(),
            AccountNumber = user.EstateAccount?.AccountNumber,
            AccountStatus = user.EstateAccount?.AccountStatus,
            BuildingName = assignment?.Room.Property.BuildingName,
            RoomNumber = assignment?.Room.RoomNumber
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null) return Unauthorized();

        user.Person.FirstName = request.FirstName.Trim();
        user.Person.LastName = request.LastName.Trim();
        user.Person.PhoneNumber = request.PhoneNumber;
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated." });
    }
}
