package za.co.campusestateliving.mobile;

import org.json.JSONArray;

import java.util.Locale;

public final class Format {
    private Format() {}

    public static String money(double amount) {
        return String.format(Locale.US, "R %,.2f", amount);
    }

    public static String when(String iso) {
        if (iso == null || iso.isEmpty() || "null".equals(iso)) return "Not set";
        String value = iso.replace('T', ' ');
        int dot = value.indexOf('.');
        if (dot > 0) value = value.substring(0, dot);
        if (value.endsWith("Z")) value = value.substring(0, value.length() - 1);
        int plus = value.indexOf('+');
        if (plus > 0) value = value.substring(0, plus);
        return value;
    }

    public static String roles(JSONArray roles) {
        if (roles == null || roles.length() == 0) return "Not available";
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < roles.length(); i++) {
            if (i > 0) builder.append(", ");
            builder.append(roles.optString(i));
        }
        return builder.toString();
    }

    public static String text(String value) {
        if (value == null || value.isEmpty() || "null".equals(value)) return "Not set";
        return value;
    }
}
