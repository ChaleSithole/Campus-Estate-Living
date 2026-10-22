using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Notices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NoticesController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public NoticesController(CampusEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnouncementDto>>> Get()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var now = DateTime.UtcNow;
        var query = _db.Announcements
            .AsNoTracking()
            .Include(a => a.CreatedByUser).ThenInclude(u => u.Person)
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > now);

        if (User.IsInRole(AppRoles.Student) || User.IsInRole(AppRoles.Staff))
            query = query.Where(a => a.Audience == AnnouncementAudiences.All || a.Audience == AnnouncementAudiences.Residents);
        else if (User.IsInRole(AppRoles.FinancialOfficer) && !User.IsInRole(AppRoles.Administrator))
            query = query.Where(a => a.Audience == AnnouncementAudiences.All || a.Audience == AnnouncementAudiences.Officers);

        var list = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<AnnouncementDto>> Create(CreateAnnouncementRequest model)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Trim().Length > 120)
            return BadRequest(new { message = "Title is required and must be 120 characters or fewer." });
        if (string.IsNullOrWhiteSpace(model.Body) || model.Body.Trim().Length > 4000)
            return BadRequest(new { message = "Notice text is required and must be 4000 characters or fewer." });
        if (!AnnouncementAudiences.AllAudiences.Contains(model.Audience))
            return BadRequest(new { message = "Choose a valid audience." });

        var notice = new Announcement
        {
            Title = model.Title.Trim(),
            Body = model.Body.Trim(),
            Audience = model.Audience,
            IsPinned = model.IsPinned,
            PublishedAt = DateTime.UtcNow,
            ExpiresAt = model.ExpiresAt,
            CreatedByUserId = userId.Value
        };
        _db.Announcements.Add(notice);
        await _db.SaveChangesAsync();

        var created = await _db.Announcements
            .AsNoTracking()
            .Include(a => a.CreatedByUser).ThenInclude(u => u.Person)
            .FirstAsync(a => a.AnnouncementId == notice.AnnouncementId);

        return Ok(ToDto(created));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Announcements.FindAsync(id);
        if (item == null) return NotFound(new { message = "Notice not found." });
        _db.Announcements.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Notice removed." });
    }

    private static AnnouncementDto ToDto(Announcement notice)
        => new()
        {
            AnnouncementId = notice.AnnouncementId,
            Title = notice.Title,
            Body = notice.Body,
            Audience = notice.Audience,
            IsPinned = notice.IsPinned,
            PublishedAt = notice.PublishedAt,
            ExpiresAt = notice.ExpiresAt,
            CreatedBy = notice.CreatedByUser.Person.FullName
        };
}
