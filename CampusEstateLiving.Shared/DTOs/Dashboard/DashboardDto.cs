using CampusEstateLiving.Shared.DTOs.Payments;

namespace CampusEstateLiving.Shared.DTOs.Dashboard;

public class DashboardDto
{
    public string UiCategory { get; set; } = Shared.UiCategory.Resident;
    public int PrimaryRoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Greeting { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UnreadNotifications { get; set; }

    public string? AccountNumber { get; set; }
    public string? AccountStatus { get; set; }
    public string? BuildingName { get; set; }
    public string? RoomNumber { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal ConfirmedThisMonth { get; set; }
    public List<ServiceChargeDto> Charges { get; set; } = [];
    public List<PaymentDto> RecentPayments { get; set; } = [];

    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int FailedCount { get; set; }
    public int ConfirmedToday { get; set; }
    public Dictionary<string, decimal> TotalsByCategory { get; set; } = [];

    public int ResidentCount { get; set; }
    public int OfficerCount { get; set; }
    public int ActiveRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int OpenMaintenance { get; set; }
    public List<DashboardUserDto> RecentUsers { get; set; } = [];
}

public class ServiceChargeDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public class DashboardUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserNumber { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; }
}
