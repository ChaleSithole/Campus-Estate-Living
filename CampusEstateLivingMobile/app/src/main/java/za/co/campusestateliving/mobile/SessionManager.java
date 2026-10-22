package za.co.campusestateliving.mobile;

import android.content.Context;
import android.content.SharedPreferences;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

/** Stores the JWT and display identity. Passwords are never stored. */
public class SessionManager {
    private static final String PREFS_NAME = "cel_session";
    private final SharedPreferences prefs;

    public SessionManager(Context context) {
        prefs = context.getApplicationContext().getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
    }

    public void saveSession(String token, String fullName, String email, String userNumber,
                            String roles, String expiresAt, int userId) {
        prefs.edit()
                .putString("token", token)
                .putString("fullName", fullName)
                .putString("email", email)
                .putString("userNumber", userNumber)
                .putString("roles", roles)
                .putString("expiresAt", expiresAt)
                .putInt("userId", userId)
                .apply();
    }

    public void updateIdentity(String fullName, String roles) {
        prefs.edit().putString("fullName", fullName).putString("roles", roles).apply();
    }

    public String getToken() { return prefs.getString("token", null); }
    public String getFullName() { return prefs.getString("fullName", ""); }
    public String getEmail() { return prefs.getString("email", ""); }
    public String getUserNumber() { return prefs.getString("userNumber", ""); }
    public String getRoles() { return prefs.getString("roles", ""); }
    public int getUserId() { return prefs.getInt("userId", 0); }
    public String getTheme() { return prefs.getString("theme", "system"); }

    public void setTheme(String theme) {
        prefs.edit().putString("theme", theme).apply();
    }

    public boolean isLoggedIn() {
        String token = getToken();
        return token != null && !token.isEmpty() && !isExpired();
    }

    public boolean isExpired() {
        String exp = prefs.getString("expiresAt", "");
        if (exp == null || exp.isEmpty()) return false;
        try {
            String value = exp;
            int dot = value.indexOf('.');
            if (dot > 0) value = value.substring(0, dot);
            if (value.endsWith("Z")) value = value.substring(0, value.length() - 1);
            int plus = value.indexOf('+');
            if (plus > 0) value = value.substring(0, plus);
            SimpleDateFormat format = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US);
            format.setTimeZone(TimeZone.getTimeZone("UTC"));
            Date when = format.parse(value);
            return when != null && when.getTime() <= System.currentTimeMillis();
        } catch (Exception ignored) {
            return false;
        }
    }

    public boolean hasRole(String role) { return Roles.has(getRoles(), role); }

    /** UI category from the roles issued at login. Never chosen by the user. */
    public int uiCategory() { return Roles.categoryFor(getRoles()); }

    public boolean isAdministratorUi() { return uiCategory() == Roles.UI_ADMIN; }
    public boolean isFinanceUi() { return uiCategory() == Roles.UI_FINANCE; }
    public boolean isResidentUi() { return uiCategory() == Roles.UI_RESIDENT; }

    public boolean isResident() {
        return hasRole(Roles.STUDENT) || hasRole(Roles.STAFF);
    }

    public boolean isStaffSide() {
        return hasRole(Roles.ADMINISTRATOR) || hasRole(Roles.FINANCIAL_OFFICER);
    }

    public void clearSession() {
        String theme = getTheme();
        prefs.edit().clear().putString("theme", theme).apply();
    }
}
