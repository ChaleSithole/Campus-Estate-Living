using System.Diagnostics;
using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using CampusEstateLiving.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Dashboard));
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        if (User.IsInRole(AppRoles.Administrator))
            return View("AdminDashboard", await BuildAdminDashboardAsync());
        if (User.IsInRole(AppRoles.FinancialOfficer))
            return View("FinancialDashboard", await BuildFinancialDashboardAsync());
        return View("ResidentDashboard", await BuildResidentDashboardAsync());
    }

    public IActionResult Privacy() => View();

    public IActionResult Help() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private async Task<ResidentDashboardViewModel> BuildResidentDashboardAsync()
    {
        var user = await CurrentUserAsync();
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var account = await _db.EstateAccounts.FirstOrDefaultAsync(a => a.UserId == user.Id);
        var assignment = await _db.RoomAssignments
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .Include(a => a.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.MoveOutDate == null);

        var payments = account == null
            ? []
            : await _db.Payments
                .Include(p => p.Category)
                .Include(p => p.PaymentStatus)
                .Where(p => p.AccountId == account.AccountId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(6)
                .ToListAsync();

        var confirmedId = await StatusId(PaymentStatusNames.Confirmed);
        var pendingId = await StatusId(PaymentStatusNames.Pending);

        var monthPayments = account == null
            ? []
            : await _db.Payments
                .Include(p => p.Category)
                .Include(p => p.PaymentStatus)
                .Where(p => p.AccountId == account.AccountId && p.PaymentDate >= start)
                .ToListAsync();

        var categories = await _db.PaymentCategories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        var serviceStatuses = categories.Select(category =>
        {
            var forCategory = monthPayments
                .Where(p => p.CategoryId == category.CategoryId)
                .ToList();
            var confirmed = forCategory.FirstOrDefault(p =>
                p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed);
            var pending = forCategory.FirstOrDefault(p =>
                p.PaymentStatus.StatusName == PaymentStatusNames.Pending);

            if (confirmed != null)
                return new ServiceStatusRow
                {
                    CategoryName = category.CategoryName,
                    Status = PaymentStatusNames.Confirmed,
                    Amount = confirmed.Amount
                };

            if (pending != null)
                return new ServiceStatusRow
                {
                    CategoryName = category.CategoryName,
                    Status = PaymentStatusNames.Pending,
                    Amount = pending.Amount
                };

            return new ServiceStatusRow
            {
                CategoryName = category.CategoryName,
                Status = "Not paid"
            };
        }).ToList();

        return new ResidentDashboardViewModel
        {
            FullName = user.Person.FullName,
            Role = User.IsInRole(AppRoles.Staff) ? "Staff" : "Student",
            Greeting = SaTime.Greeting(),
            AccountNumber = account?.AccountNumber,
            AccountStatus = account?.AccountStatus,
            RoomLabel = assignment == null ? null : $"{assignment.Room.Property.BuildingName} · Room {assignment.Room.RoomNumber}",
            PropertyName = assignment?.Room.Property.BuildingName,
            ConfirmedThisMonth = account == null ? 0 : await _db.Payments
                .Where(p => p.AccountId == account.AccountId && p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            PendingTotal = account == null ? 0 : await _db.Payments
                .Where(p => p.AccountId == account.AccountId && p.PaymentStatusId == pendingId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            UnreadNotifications = await _db.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead),
            RecentPayments = payments,
            ServiceStatuses = serviceStatuses,
            LatestNotifications = await _db.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync(),
            Notices = await ActiveNotices(AnnouncementAudiences.Residents),
            OpenMaintenance = await _db.MaintenanceRequests.CountAsync(m =>
                m.UserId == user.Id &&
                (m.Status == MaintenanceStatuses.Open || m.Status == MaintenanceStatuses.InProgress))
        };
    }

    private async Task<AdminDashboardViewModel> BuildAdminDashboardAsync()
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var confirmedId = await StatusId(PaymentStatusNames.Confirmed);
        var pendingId = await StatusId(PaymentStatusNames.Pending);

        var occupied = await _db.RoomAssignments
            .Where(a => a.MoveOutDate == null)
            .Select(a => a.RoomId)
            .Distinct()
            .CountAsync();

        var byCategory = await _db.Payments
            .Include(p => p.Category)
            .Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
            .GroupBy(p => p.Category.CategoryName)
            .Select(g => new { g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

        return new AdminDashboardViewModel
        {
            ResidentCount = await _db.Users.CountAsync(u => u.EstateAccount != null),
            OfficerCount = (await _users.GetUsersInRoleAsync(AppRoles.FinancialOfficer)).Count,
            ActiveRooms = await _db.Rooms.CountAsync(),
            OccupiedRooms = occupied,
            PendingPayments = await _db.Payments.CountAsync(p => p.PaymentStatusId == pendingId),
            ConfirmedThisMonth = await _db.Payments
                .Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            PendingAmount = await _db.Payments
                .Where(p => p.PaymentStatusId == pendingId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            RecentPayments = await _db.Payments
                .Include(p => p.Category)
                .Include(p => p.PaymentStatus)
                .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
                .OrderByDescending(p => p.PaymentDate)
                .Take(8)
                .ToListAsync(),
            RecentUsers = await _db.Users
                .Include(u => u.Person)
                .OrderByDescending(u => u.CreatedAt)
                .Take(6)
                .ToListAsync(),
            TotalsByCategory = byCategory,
            OpenMaintenance = await _db.MaintenanceRequests.CountAsync(m =>
                m.Status == MaintenanceStatuses.Open || m.Status == MaintenanceStatuses.InProgress),
            Notices = await ActiveNotices(AnnouncementAudiences.All)
        };
    }

    private async Task<FinancialDashboardViewModel> BuildFinancialDashboardAsync()
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var today = DateTime.UtcNow.Date;
        var confirmedId = await StatusId(PaymentStatusNames.Confirmed);
        var pendingId = await StatusId(PaymentStatusNames.Pending);
        var failedId = await StatusId(PaymentStatusNames.Failed);

        var byCategory = await _db.Payments
            .Include(p => p.Category)
            .Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
            .GroupBy(p => p.Category.CategoryName)
            .Select(g => new { g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

        return new FinancialDashboardViewModel
        {
            PendingCount = await _db.Payments.CountAsync(p => p.PaymentStatusId == pendingId),
            PendingAmount = await _db.Payments.Where(p => p.PaymentStatusId == pendingId).SumAsync(p => (decimal?)p.Amount) ?? 0,
            ConfirmedToday = await _db.Payments.CountAsync(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= today),
            ConfirmedThisMonth = await _db.Payments
                .Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            FailedCount = await _db.Payments.CountAsync(p => p.PaymentStatusId == failedId),
            Queue = await _db.Payments
                .Include(p => p.Category)
                .Include(p => p.PaymentStatus)
                .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person)
                .Where(p => p.PaymentStatusId == pendingId)
                .OrderBy(p => p.PaymentDate)
                .Take(12)
                .ToListAsync(),
            TotalsByCategory = byCategory,
            OpenMaintenance = await _db.MaintenanceRequests.CountAsync(m =>
                m.Status == MaintenanceStatuses.Open || m.Status == MaintenanceStatuses.InProgress)
        };
    }

    private async Task<ApplicationUser> CurrentUserAsync()
    {
        var user = await _users.GetUserAsync(User)
            ?? throw new InvalidOperationException("User not found.");
        await _db.Entry(user).Reference(u => u.Person).LoadAsync();
        return user;
    }

    private Task<int> StatusId(string name)
        => _db.PaymentStatuses.Where(s => s.StatusName == name).Select(s => s.PaymentStatusId).SingleAsync();

    private async Task<List<Announcement>> ActiveNotices(string audience)
    {
        var now = DateTime.UtcNow;
        return await _db.Announcements
            .Where(a =>
                (a.ExpiresAt == null || a.ExpiresAt > now) &&
                (a.Audience == AnnouncementAudiences.All || a.Audience == audience))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .Take(3)
            .ToListAsync();
    }
}
