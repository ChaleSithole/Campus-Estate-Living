package za.co.campusestateliving.mobile.api;

import android.content.SharedPreferences;

import java.net.URI;

import za.co.campusestateliving.mobile.BuildConfig;
import za.co.campusestateliving.mobile.CelApp;
import za.co.campusestateliving.mobile.SessionManager;

/**
 * Resolves the API base URL.
 *
 * Release builds always return BuildConfig.DEFAULT_API_BASE (set in build.gradle).
 * Debug builds may return a value saved by the developer settings dialog, so a
 * phone can reach a PC on the LAN without a rebuild.
 */
public final class ApiBaseUrl {
    private static final String PREFS = "cel_api";
    private static final String KEY_OVERRIDE = "base_url_override";

    private ApiBaseUrl() {}

    public static String defaultUrl() {
        return BuildConfig.DEFAULT_API_BASE;
    }

    /** Always ends with a single slash, e.g. http://10.0.2.2:5058/ */
    public static String current() {
        if (BuildConfig.DEBUG) {
            String override = prefs().getString(KEY_OVERRIDE, null);
            if (override != null && !override.isEmpty()) return override;
        }
        return defaultUrl();
    }

    public static boolean isOverridden() {
        return BuildConfig.DEBUG && prefs().contains(KEY_OVERRIDE);
    }

    /** Debug builds only. Throws IllegalArgumentException with a message the user can read. */
    public static void setOverride(String raw) {
        requireDebug();
        String url = normalise(raw);
        String before = current();
        prefs().edit().putString(KEY_OVERRIDE, url).apply();
        if (!url.equals(before)) new SessionManager(CelApp.context()).clearSession();
    }

    /** Debug builds only. Returns to the build default. */
    public static void resetOverride() {
        requireDebug();
        String before = current();
        prefs().edit().remove(KEY_OVERRIDE).apply();
        if (!defaultUrl().equals(before)) new SessionManager(CelApp.context()).clearSession();
    }

    /** Accepts "192.168.1.20:5058", "http://10.0.2.2:5058" or a full URL. Rejects paths. */
    static String normalise(String raw) {
        String s = raw == null ? "" : raw.trim();
        if (s.isEmpty()) throw new IllegalArgumentException("Enter the server address.");
        if (!s.startsWith("http://") && !s.startsWith("https://")) s = "http://" + s;
        URI uri;
        try {
            uri = new URI(s);
        } catch (Exception e) {
            throw new IllegalArgumentException("That address is not valid.");
        }
        if (uri.getHost() == null) {
            throw new IllegalArgumentException("Enter a host, for example 192.168.1.20:5058");
        }
        String path = uri.getPath();
        if (path != null && !path.isEmpty() && !"/".equals(path)) {
            throw new IllegalArgumentException("Enter the server address only, without a path.");
        }
        int port = uri.getPort();
        return uri.getScheme() + "://" + uri.getHost() + (port > 0 ? ":" + port : "") + "/";
    }

    private static void requireDebug() {
        if (!BuildConfig.DEBUG) {
            throw new IllegalStateException("The server override is available in debug builds only.");
        }
    }

    private static SharedPreferences prefs() {
        return CelApp.context().getSharedPreferences(PREFS, android.content.Context.MODE_PRIVATE);
    }
}
