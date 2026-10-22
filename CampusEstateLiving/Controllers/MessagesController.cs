using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.FinancialOfficer}")]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index(int? withUserId)
    {
        var current = await _users.GetUserAsync(User);
        if (current == null) return Challenge();
        var staff = await _db.Users.AsNoTracking().Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(r => r.Role)
            .Where(u => u.IsActive && u.Id != current.Id && u.UserRoles.Any(r =>
                r.Role.Name == AppRoles.Administrator || r.Role.Name == AppRoles.FinancialOfficer))
            .OrderBy(u => u.Person.LastName).ThenBy(u => u.Person.FirstName).ToListAsync();
        var ids = staff.Select(u => u.Id).ToList();
        var rows = await _db.StaffMessages.AsNoTracking()
            .Where(m => (m.SenderUserId == current.Id && ids.Contains(m.RecipientUserId)) ||
                        (m.RecipientUserId == current.Id && ids.Contains(m.SenderUserId)))
            .OrderByDescending(m => m.SentAt).ToListAsync();
        var model = new StaffMessagesIndexViewModel();
        foreach (var peer in staff)
        {
            var thread = rows.Where(m => (m.SenderUserId == current.Id && m.RecipientUserId == peer.Id) ||
                                         (m.SenderUserId == peer.Id && m.RecipientUserId == current.Id))
                .OrderBy(m => m.SentAt).ToList();
            var latest = thread.LastOrDefault();
            model.Contacts.Add(new StaffMessageContact
            {
                UserId = peer.Id,
                Name = peer.Person.FullName,
                Role = peer.UserRoles.Any(r => r.Role.Name == AppRoles.Administrator) ? "Administrator" : "Financial Officer",
                LatestMessage = latest?.Body,
                LatestAt = latest?.SentAt,
                UnreadCount = thread.Count(m => m.RecipientUserId == current.Id && m.ReadAt == null)
            });
        }
        model.Contacts = model.Contacts.OrderByDescending(c => c.LatestAt ?? DateTime.MinValue).ThenBy(c => c.Name).ToList();

        if (withUserId.HasValue)
        {
            var peer = staff.FirstOrDefault(u => u.Id == withUserId.Value);
            if (peer == null) return NotFound();
            var unread = await _db.StaffMessages.Where(m => m.SenderUserId == peer.Id && m.RecipientUserId == current.Id && m.ReadAt == null).ToListAsync();
            foreach (var message in unread) message.ReadAt = DateTime.UtcNow;
            if (unread.Count > 0) await _db.SaveChangesAsync();
            var messages = await _db.StaffMessages.AsNoTracking().Where(m =>
                    (m.SenderUserId == current.Id && m.RecipientUserId == peer.Id) ||
                    (m.SenderUserId == peer.Id && m.RecipientUserId == current.Id))
                .OrderBy(m => m.SentAt).ToListAsync();
            model.Selected = new StaffConversationSummary
            {
                UserId = peer.Id,
                Name = peer.Person.FullName,
                Role = peer.UserRoles.Any(r => r.Role.Name == AppRoles.Administrator) ? "Administrator" : "Financial Officer",
                Messages = messages
            };
        }
        ViewBag.CurrentUserId = current.Id;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int recipientId, string? body)
    {
        var current = await _users.GetUserAsync(User);
        if (current == null) return Challenge();
        var recipient = await _db.Users.Include(u => u.UserRoles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Id == recipientId && u.IsActive && u.UserRoles.Any(r =>
                r.Role.Name == AppRoles.Administrator || r.Role.Name == AppRoles.FinancialOfficer));
        var message = body?.Trim();
        if (recipient == null || recipient.Id == current.Id) return NotFound();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 4000)
        {
            TempData["Error"] = "Write a message of 1 to 4,000 characters.";
            return RedirectToAction(nameof(Index), new { withUserId = recipientId });
        }
        _db.StaffMessages.Add(new StaffMessage
        {
            SenderUserId = current.Id,
            RecipientUserId = recipient.Id,
            Body = message,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { withUserId = recipientId });
    }
}
