using CampusEstateLiving.Api.Data;
using CampusEstateLiving.Api.Data.Entities;
using CampusEstateLiving.Api.Services;
using CampusEstateLiving.Shared;
using CampusEstateLiving.Shared.DTOs.Dashboard;
using CampusEstateLiving.Shared.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEstateLiving.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly CampusEstateDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DashboardController(CampusEstateDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        if (User.IsInRole(AppRoles.Administrator))
            return Ok(await AdminAsync(userId.Value));
        if (User.IsInRole(AppRoles.FinancialOfficer))
            return Ok(await FinanceAsync(userId.Value));
        return Ok(await ResidentAsync(userId.Value));
    }

    private async Task<DashboardDto> ResidentAsync(int userId)
    {
        var user = await _users.Users.Include(u => u.Person).FirstAsync(u => u.Id == userId);
        var roles = await _users.GetRolesAsync(user);
        var start = SaClock.MonthStartUtc();
        var account = await _db.EstateAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);
        var assignment = await _db.RoomAssignments
            .AsNoTracking()
            .Include(a => a.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.MoveOutDate == null);

        var pendingId = await StatusId(PaymentStatusNames.Pending);
        var confirmedId = await StatusId(PaymentStatusNames.Confirmed);

        var outstanding = account == null ? 0 : await _db.Payments
            .Where(p => p.AccountId == account.AccountId && p.PaymentStatusId == pendingId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        var confirmed = account == null ? 0 : await _db.Payments
            .Where(p => p.AccountId == account.AccountId && p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var monthPayments = account == null
            ? new List<Payment>()
            : await _db.Payments
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.PaymentStatus)
                .Where(p => p.AccountId == account.AccountId && p.PaymentDate >= start)
                .ToListAsync();

        var categories = await _db.PaymentCategories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
        var charges = categories.Select(category =>
        {
            var rows = monthPayments.Where(p => p.CategoryId == category.CategoryId).ToList();
            var paid = rows.FirstOrDefault(p => p.PaymentStatus.StatusName == PaymentStatusNames.Confirmed);
            var waiting = rows.FirstOrDefault(p => p.PaymentStatus.StatusName == PaymentStatusNames.Pending);
            if (paid != null)
                return new ServiceChargeDto { CategoryName = category.CategoryName, Status = PaymentStatusNames.Confirmed, Amount = paid.Amount };
            if (waiting != null)
                return new ServiceChargeDto { CategoryName = category.CategoryName, Status = PaymentStatusNames.Pending, Amount = waiting.Amount };
            return new ServiceChargeDto { CategoryName = category.CategoryName, Status = "Not paid" };
        }).ToList();

        var recent = account == null
            ? new List<Payment>()
            : await PaymentsQuery()
                .Where(p => p.AccountId == account.AccountId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

        return new DashboardDto
        {
            UiCategory = UiCategory.Resident,
            PrimaryRoleId = UiCategory.PrimaryRoleId(roles),
            FullName = user.Person.FullName,
            Greeting = SaClock.Greeting(),
            IsActive = user.IsActive,
            UnreadNotifications = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
            AccountNumber = account?.AccountNumber,
            AccountStatus = account?.AccountStatus,
            BuildingName = assignment?.Room.Property.BuildingName,
            RoomNumber = assignment?.Room.RoomNumber,
            OutstandingBalance = outstanding,
            ConfirmedThisMonth = confirmed,
            Charges = charges,
            RecentPayments = recent.Select(PaymentMapper.ToDto).ToList(),
            OpenMaintenance = await _db.MaintenanceRequests.CountAsync(m =>
                m.UserId == userId &&
                (m.Status == MaintenanceStatuses.Open || m.Status == MaintenanceStatuses.InProgress))
        };
    }

    private async Task<DashboardDto> FinanceAsync(int userId)
    {
        var user = await _users.Users.Include(u => u.Person).FirstAsync(u => u.Id == userId);
        var start = SaClock.MonthStartUtc();
        var today = DateTime.UtcNow.Date;
        var pendingId = await StatusId(PaymentStatusNames.Pending);
        var confirmedId = await StatusId(PaymentStatusNames.Confirmed);
        var failedId = await StatusId(PaymentStatusNames.Failed);

        var recent = await PaymentsQuery()
            .OrderByDescending(p => p.PaymentDate)
            .Take(8)
            .ToListAsync();

        return new DashboardDto
        {
            UiCategory = UiCategory.FinancialOfficer,
            PrimaryRoleId = UiCategory.FinancialOfficerRoleId,
            FullName = user.Person.FullName,
            Greeting = SaClock.Greeting(),
            IsActive = user.IsActive,
            UnreadNotifications = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
            PendingCount = await _db.Payments.CountAsync(p => p.PaymentStatusId == pendingId),
            PendingAmount = await _db.Payments.Where(p => p.PaymentStatusId == pendingId).SumAsync(p => (decimal?)p.Amount) ?? 0,
            FailedCount = await _db.Payments.CountAsync(p => p.PaymentStatusId == failedId),
            ConfirmedToday = await _db.Payments.CountAsync(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= today),
            ConfirmedThisMonth = await _db.Payments.Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start).SumAsync(p => (decimal?)p.Amount) ?? 0,
            TotalsByCategory = await CategoryTotals(confirmedId, start),
            RecentPayments = recent.Select(PaymentMapper.ToDto).ToList(),
            OpenMaintenance = await _db.MaintenanceRequests.CountAsync(m =>
                m.Status == MaintenanceStatuses.Open || m.Status == MaintenanceStatuses.InProgress)
        };
    }

    private async Task<DashboardDto> AdminAsync(int userId)
    {
        var user = await _users.Users.Include(u => u.Person).FirstAsync(u => u.Id == userId);
        var start = SaClock.MonthStartUtc();
        var pendingId = await StatusId(PaymentStatusNames.Pending);
        var confirmedId = await StatusId(PaymentStatusNames.Confirmed);
        var occupied = await _db.RoomAssignments
            .Where(a => a.MoveOutDate == null)
            .Select(a => a.RoomId)
            .Distinct()
            .CountAsync();

        var officerRoleId = await _db.Roles
            .Where(r => r.Name == AppRoles.FinancialOfficer)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var recentUsers = await _db.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Take(6)
            .ToListAsync();

        var recent = await PaymentsQuery()
            .OrderByDescending(p => p.PaymentDate)
            .Take(5)
            .ToListAsync();

        return new DashboardDto
        {
            UiCategory = UiCategory.Administrator,
            PrimaryRoleId = UiCategory.AdministratorRoleId,
            FullName = user.Person.FullName,
            Greeting = SaClock.Greeting(),
            IsActive = user.IsActive,
            UnreadNotifications = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
            ResidentCount = await _db.Users.CountAsync(u => u.EstateAccount != null),
            OfficerCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == officerRoleId),
            ActiveRooms = await _db.Rooms.CountAsync(),
            OccupiedRooms = occupied,
            PendingCount = await _db.Payments.CountAsync(p => p.PaymentStatusId == pendingId),
            PendingAmount = await _db.Payments.Where(p => p.PaymentStatusId == pendingId).SumAsync(p => (decimal?)p.Amount) ?? 0,
            ConfirmedThisMonth = await _db.Payments.Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start).SumAsync(p => (decimal?)p.Amount) ?? 0,
            OpenMaintenance = await _db.MaintenanceRequests.CountAsync(m =>
                m.Status == MaintenanceStatuses.Open || m.Status == MaintenanceStatuses.InProgress),
            TotalsByCategory = await CategoryTotals(confirmedId, start),
            RecentPayments = recent.Select(PaymentMapper.ToDto).ToList(),
            RecentUsers = recentUsers.Select(u => new DashboardUserDto
            {
                UserId = u.Id,
                FullName = u.Person.FullName,
                UserNumber = u.UserNumber,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name ?? string.Empty).Where(n => n.Length > 0).ToList()
            }).ToList()
        };
    }

    private async Task<Dictionary<string, decimal>> CategoryTotals(int confirmedId, DateTime start)
    {
        var rows = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentStatusId == confirmedId && p.PaymentDate >= start)
            .GroupBy(p => p.Category.CategoryName)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();
        return rows.ToDictionary(x => x.Name, x => x.Total);
    }

    private Task<int> StatusId(string name)
        => _db.PaymentStatuses.Where(s => s.StatusName == name).Select(s => s.PaymentStatusId).SingleAsync();

    private IQueryable<Payment> PaymentsQuery()
        => _db.Payments
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PaymentStatus)
            .Include(p => p.SavedCard)
            .Include(p => p.ProcessedByUser!).ThenInclude(u => u.Person)
            .Include(p => p.Account).ThenInclude(a => a.User).ThenInclude(u => u.Person);
}
