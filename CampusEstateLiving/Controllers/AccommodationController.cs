using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

[Authorize(Roles = $"{AppRoles.Student},{AppRoles.Staff}")]
public class AccommodationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AccommodationController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(_users.GetUserId(User)!);
        var current = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .Include(a => a.Room).ThenInclude(r => r.RoomType)
            .Include(a => a.User).ThenInclude(u => u.Person)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.MoveOutDate == null);

        var history = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .Include(a => a.Room).ThenInclude(r => r.RoomType)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.MoveInDate)
            .ToListAsync();

        List<ApplicationUser> roommates = [];
        if (current != null)
        {
            roommates = await _db.RoomAssignments
                .Include(a => a.User).ThenInclude(u => u.Person)
                .Where(a => a.RoomId == current.RoomId && a.MoveOutDate == null && a.UserId != userId)
                .Select(a => a.User)
                .ToListAsync();
        }

        ViewBag.Current = current;
        ViewBag.Roommates = roommates;
        ViewBag.Account = await _db.EstateAccounts.FirstOrDefaultAsync(a => a.UserId == userId);
        var accountId = await _db.EstateAccounts.Where(a => a.UserId == userId).Select(a => (int?)a.AccountId).FirstOrDefaultAsync();
        ViewBag.MonthlyRent = accountId == null ? 0m : await _db.Payments
            .Where(p => p.AccountId == accountId && p.Category.CategoryName == PaymentCategoryNames.Rent && p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed)
            .OrderByDescending(p => p.PaymentDate).Select(p => p.Amount).FirstOrDefaultAsync();
        return View(history);
    }
}
