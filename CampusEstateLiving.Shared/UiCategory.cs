namespace CampusEstateLiving.Shared;

/// <summary>
/// Product role ids used by Campus Estate Living.
/// 1 Administrator, 2 Financial Officer, 3 Student, 4 Staff Member.
/// Student and Staff share the Resident UI. Identity surrogate keys are separate.
/// </summary>
public static class UiCategory
{
    public const string Administrator = "Administrator";
    public const string FinancialOfficer = "FinancialOfficer";
    public const string Resident = "Resident";

    public const int AdministratorRoleId = 1;
    public const int FinancialOfficerRoleId = 2;
    public const int StudentRoleId = 3;
    public const int StaffRoleId = 4;

    public static string ForRoles(IEnumerable<string> roles)
    {
        if (Has(roles, AppRoles.Administrator)) return Administrator;
        if (Has(roles, AppRoles.FinancialOfficer)) return FinancialOfficer;
        return Resident;
    }

    public static int PrimaryRoleId(IEnumerable<string> roles)
    {
        if (Has(roles, AppRoles.Administrator)) return AdministratorRoleId;
        if (Has(roles, AppRoles.FinancialOfficer)) return FinancialOfficerRoleId;
        if (Has(roles, AppRoles.Staff)) return StaffRoleId;
        return StudentRoleId;
    }

    private static bool Has(IEnumerable<string> roles, string expected)
        => roles.Any(role => string.Equals(role, expected, StringComparison.OrdinalIgnoreCase));
}
