package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.widget.LinearLayout;
import android.widget.Toast;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public abstract class CelActivity extends Activity {
    protected SessionManager session;
    protected final ExecutorService executor = Executors.newSingleThreadExecutor();
    protected LinearLayout root;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        session = new SessionManager(this);
    }

    protected boolean gate() {
        if (session.isLoggedIn()) return true;
        session.clearSession();
        Intent intent = new Intent(this, LoginActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
        finish();
        return false;
    }

    protected boolean allowCategory(int... allowed) {
        if (!gate()) return false;
        int mine = session.uiCategory();
        for (int category : allowed) {
            if (category == mine) return true;
        }
        toast("This screen is not available for your role.");
        finish();
        return false;
    }

    protected void bind(String tab) {
        root = Ui.screen(this, tab);
    }

    protected void io(Job job) {
        executor.execute(() -> {
            try {
                job.run();
            } catch (Exception error) {
                runOnUiThread(() -> onError(error));
            }
        });
    }

    protected void onError(Exception error) {
        if (error instanceof ApiClient.ApiException && ((ApiClient.ApiException) error).statusCode == 401) {
            session.clearSession();
            toast("Your session has ended. Sign in again.");
            Intent intent = new Intent(this, LoginActivity.class);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
            startActivity(intent);
            finish();
            return;
        }
        String message = error.getMessage();
        toast(message == null || message.isEmpty() ? "Could not reach the estate API." : message);
    }

    protected void toast(String message) {
        Toast.makeText(this, message, Toast.LENGTH_LONG).show();
    }

    protected void open(Class<?> activity) {
        startActivity(new Intent(this, activity));
    }

    protected interface Job {
        void run() throws Exception;
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        executor.shutdownNow();
    }
}
