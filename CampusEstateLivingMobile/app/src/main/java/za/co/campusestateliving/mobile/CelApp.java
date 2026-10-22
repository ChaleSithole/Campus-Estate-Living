package za.co.campusestateliving.mobile;

import android.app.Application;
import android.content.Context;

/** Keeps the application Context so static helpers (ApiBaseUrl) can read preferences. */
public class CelApp extends Application {
    private static Context appContext;

    @Override
    public void onCreate() {
        super.onCreate();
        appContext = getApplicationContext();
    }

    public static Context context() {
        return appContext;
    }
}
