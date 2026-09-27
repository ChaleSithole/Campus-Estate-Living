namespace CampusEstateLiving.Shared;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string FinancialOfficer = "FinancialOfficer";
    public const string Student = "Student";
    public const string Staff = "Staff";

    public static readonly string[] All =
    [
        Administrator, FinancialOfficer, Student, Staff
    ];

    public static readonly string[] Resident =
    [
        Student, Staff
    ];

    public static readonly string[] StaffSide =
    [
        Administrator, FinancialOfficer
    ];
}

public static class AccountStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Suspended = "Suspended";
}

public static class PaymentStatusNames
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class PaymentCategoryNames
{
    public const string Rent = "Rent";
    public const string Water = "Water";
    public const string Electricity = "Electricity";
    public const string Parking = "Parking";
    public const string RefuseCollection = "Refuse Collection";
}

public static class PaymentMethods
{
    public const string Card = "Card";
    public const string Eft = "EFT";
    public const string Cash = "Cash";
}

public static class RoomTypeNames
{
    public const string Single = "Single";
    public const string Sharing = "Sharing";
}
