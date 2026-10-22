using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/StaffMessages")]
[Authorize(Roles = AppRoles.Administrator + "," + AppRoles.FinancialOfficer)]
public class StaffMessagesController : ControllerBase
{
    private readonly CampusEstateDbContext _db;

    public StaffMessagesController(CampusEstateDbContext db) => _db = db;

    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var users = await _db.Users.AsNoTracking().Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive && u.Id != userId.Value && u.UserRoles.Any(ur =>
                ur.Role.Name == AppRoles.Administrator || ur.Role.Name == AppRoles.FinancialOfficer))
            .OrderBy(u => u.Person.LastName).ThenBy(u => u.Person.FirstName).ToListAsync();
        var ids = users.Select(u => u.Id).ToList();
        var messages = await _db.StaffMessages.AsNoTracking()
            .Where(m => (m.SenderUserId == userId && ids.Contains(m.RecipientUserId)) ||
                        (m.RecipientUserId == userId && ids.Contains(m.SenderUserId)))
            .OrderByDescending(m => m.SentAt).ToListAsync();
        return Ok(users.Select(u => new
        {
            userId = u.Id,
            fullName = u.Person.FullName,
            role = u.UserRoles.Any(r => r.Role.Name == AppRoles.Administrator) ? AppRoles.Administrator : AppRoles.FinancialOfficer,
            latestMessage = messages.FirstOrDefault(m => m.SenderUserId == u.Id || m.RecipientUserId == u.Id)?.Body,
            unreadCount = messages.Count(m => m.SenderUserId == u.Id && m.RecipientUserId == userId && m.ReadAt == null)
        }).OrderByDescending(c => c.latestMessage != null));
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var rows = await _db.StaffMessages.AsNoTracking()
            .Where(m => m.SenderUserId == userId || m.RecipientUserId == userId)
            .OrderByDescending(m => m.SentAt).ToListAsync();
        var peerIds = rows.Select(m => m.SenderUserId == userId ? m.RecipientUserId : m.SenderUserId).Distinct().ToList();
        var peers = await _db.Users.AsNoTracking().Include(u => u.Person).Include(u => u.UserRoles).ThenInclude(r => r.Role)
            .Where(u => peerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        var result = rows.GroupBy(m => m.SenderUserId == userId ? m.RecipientUserId : m.SenderUserId)
            .Where(g => peers.ContainsKey(g.Key)).Select(g =>
            {
                var peer = peers[g.Key];
                var latest = g.OrderByDescending(m => m.SentAt).First();
                return new
                {
                    userId = peer.Id,
                    fullName = peer.Person.FullName,
                    role = peer.UserRoles.Any(r => r.Role.Name == AppRoles.Administrator) ? AppRoles.Administrator : AppRoles.FinancialOfficer,
                    latestMessage = latest.Body,
                    latestAt = latest.SentAt,
                    unreadCount = g.Count(m => m.RecipientUserId == userId && m.ReadAt == null)
                };
            }).OrderByDescending(c => c.latestAt).ToList();
        return Ok(result);
    }

    [HttpGet("{peerId:int}")]
    public async Task<IActionResult> Thread(int peerId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var peer = await FindPeer(peerId);
        if (peer is null) return NotFound(new { message = "Administrator or financial officer not found." });
        var unread = await _db.StaffMessages.Where(m => m.SenderUserId == peerId && m.RecipientUserId == userId && m.ReadAt == null).ToListAsync();
        foreach (var message in unread) message.ReadAt = DateTime.UtcNow;
        if (unread.Count > 0) await _db.SaveChangesAsync();
        var messages = await _db.StaffMessages.AsNoTracking()
            .Where(m => (m.SenderUserId == userId && m.RecipientUserId == peerId) || (m.SenderUserId == peerId && m.RecipientUserId == userId))
            .OrderBy(m => m.SentAt).Select(m => new
            {
                messageId = m.MessageId, senderUserId = m.SenderUserId, recipientUserId = m.RecipientUserId,
                body = m.Body, sentAt = m.SentAt, readAt = m.ReadAt
            }).ToListAsync();
        return Ok(new
        {
            peer = new { userId = peer.Id, fullName = peer.Person.FullName, role = peer.UserRoles.Any(r => r.Role.Name == AppRoles.Administrator) ? AppRoles.Administrator : AppRoles.FinancialOfficer },
            messages
        });
    }

    [HttpPost("{peerId:int}")]
    public async Task<IActionResult> Send(int peerId, [FromBody] SendMessageRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var peer = await FindPeer(peerId);
        if (peer is null) return NotFound(new { message = "Administrator or financial officer not found." });
        var body = request.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest(new { message = "Write a message before sending." });
        if (body.Length > 4000) return BadRequest(new { message = "Messages can be at most 4,000 characters." });
        var message = new StaffMessage { SenderUserId = userId.Value, RecipientUserId = peerId, Body = body, SentAt = DateTime.UtcNow };
        _db.StaffMessages.Add(message);
        await _db.SaveChangesAsync();
        return Ok(new { messageId = message.MessageId, sentAt = message.SentAt });
    }

    private Task<ApplicationUser?> FindPeer(int id) => _db.Users.Include(u => u.Person)
        .Include(u => u.UserRoles).ThenInclude(r => r.Role)
        .FirstOrDefaultAsync(u => u.Id == id && u.IsActive && u.UserRoles.Any(r =>
            r.Role.Name == AppRoles.Administrator || r.Role.Name == AppRoles.FinancialOfficer));

    public sealed class SendMessageRequest
    {
        public string? Body { get; set; }
    }
}
