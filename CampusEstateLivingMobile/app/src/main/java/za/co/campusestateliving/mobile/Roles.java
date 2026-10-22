package za.co.campusestateliving.mobile;

/** Role names from CampusEstateLiving.Shared.AppRoles. Do not rename them. */
public final class Roles {
    public static final String ADMINISTRATOR = "Administrator";
    public static final String FINANCIAL_OFFICER = "FinancialOfficer";
    public static final String STUDENT = "Student";
    public static final String STAFF = "Staff";

    public static final int UI_ADMIN = 1;
    public static final int UI_FINANCE = 2;
    public static final int UI_RESIDENT = 3;

    private Roles() {}

    public static boolean has(String roles, String expected) {
        if (roles == null || expected == null) return false;
        for (String role : roles.split(",")) {
            if (role.trim().equalsIgnoreCase(expected)) return true;
        }
        return false;
    }

    /** Administrator wins, then Financial Officer, otherwise the shared Resident UI. */
    public static int categoryFor(String roles) {
        if (has(roles, ADMINISTRATOR)) return UI_ADMIN;
        if (has(roles, FINANCIAL_OFFICER)) return UI_FINANCE;
        return UI_RESIDENT;
    }
}
