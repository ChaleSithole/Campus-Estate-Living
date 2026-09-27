using CampusEstateLiving.Models;

namespace CampusEstateLiving.ViewModels;

public class ResidentDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? AccountStatus { get; set; }
    public string? RoomLabel { get; set; }
    public string? PropertyName { get; set; }
    public decimal ConfirmedThisMonth { get; set; }
    public decimal PendingTotal { get; set; }
    public int UnreadNotifications { get; set; }
    public List<Payment> RecentPayments { get; set; } = [];
    public List<Notification> LatestNotifications { get; set; } = [];
    public List<ServiceStatusRow> ServiceStatuses { get; set; } = [];
    public string Greeting { get; set; } = "Hello";
    public List<Announcement> Notices { get; set; } = [];
    public int OpenMaintenance { get; set; }
}

public class ServiceStatusRow
{
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = "Not paid";
    public decimal? Amount { get; set; }
}

public class AdminDashboardViewModel
{
    public int ResidentCount { get; set; }
    public int OfficerCount { get; set; }
    public int ActiveRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int PendingPayments { get; set; }
    public decimal ConfirmedThisMonth { get; set; }
    public decimal PendingAmount { get; set; }
    public List<Payment> RecentPayments { get; set; } = [];
    public List<ApplicationUser> RecentUsers { get; set; } = [];
    public Dictionary<string, decimal> TotalsByCategory { get; set; } = [];
    public int OpenMaintenance { get; set; }
    public List<Announcement> Notices { get; set; } = [];
}

public class FinancialDashboardViewModel
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int ConfirmedToday { get; set; }
    public decimal ConfirmedThisMonth { get; set; }
    public int FailedCount { get; set; }
    public List<Payment> Queue { get; set; } = [];
    public Dictionary<string, decimal> TotalsByCategory { get; set; } = [];
    public int OpenMaintenance { get; set; }
}

public class ReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public List<Payment> Payments { get; set; } = [];
    public decimal Total { get; set; }
    public Dictionary<string, decimal> ByCategory { get; set; } = [];
    public Dictionary<string, int> ByStatus { get; set; } = [];
}
