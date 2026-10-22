package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.app.AlertDialog;
import android.content.Intent;
import android.view.Gravity;
import android.widget.TextView;
import za.co.campusestateliving.mobile.api.ApiBaseUrl;
import android.os.Bundle;
import android.text.method.PasswordTransformationMethod;
import android.widget.Button;
import android.widget.EditText;

import za.co.campusestateliving.mobile.model.LoginRequest;
import za.co.campusestateliving.mobile.model.LoginResponse;

public class LoginActivity extends CelActivity {
    private EditText emailInput;
    private EditText passwordInput;
    private Button loginButton;
    private TextView loginError;
    private boolean busy;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (session.isLoggedIn()) {
            openDashboard();
            return;
        }
        bind(null);
        getLayoutInflater().inflate(R.layout.screen_login, root, true);

        emailInput = findViewById(R.id.emailInput);
        passwordInput = findViewById(R.id.passwordInput);
        loginButton = findViewById(R.id.loginButton);
        loginError = findViewById(R.id.loginError);
        Button show = findViewById(R.id.showPasswordButton);
        Button register = findViewById(R.id.registerButton);

        show.setOnClickListener(v -> {
            boolean hidden = passwordInput.getTransformationMethod() != null;
            passwordInput.setTransformationMethod(hidden ? null : PasswordTransformationMethod.getInstance());
            show.setText(hidden ? "Hide password" : "Show password");
            passwordInput.setSelection(passwordInput.getText().length());
        });
        loginButton.setOnClickListener(v -> signIn());
        register.setOnClickListener(v -> startActivity(new Intent(this, RegisterActivity.class)));

        // Developer-only server setting. Compiled into debug builds; release builds never show it.
        if (BuildConfig.DEBUG) addServerSetting();
    }

    private void addServerSetting() {
        TextView label = Ui.text(this, serverLabel(), 11, Ui.muted(this));
        label.setGravity(Gravity.CENTER);
        label.setPadding(0, Ui.dp(this, 8), 0, Ui.dp(this, 8));
        label.setOnClickListener(v -> showServerDialog(label));
        findViewById(R.id.loginMark).setOnLongClickListener(v -> {
            showServerDialog(label);
            return true;
        });
    }

    private String serverLabel() {
        return "API: " + ApiBaseUrl.current() + (ApiBaseUrl.isOverridden() ? "  (debug override)" : "");
    }

    private void showServerDialog(TextView label) {
        EditText input = new EditText(this);
        input.setSingleLine(true);
        input.setText(ApiBaseUrl.current());
        input.setSelection(input.getText().length());
        new AlertDialog.Builder(this)
                .setTitle("API server (debug only)")
                .setMessage("Emulator: http://10.0.2.2:5058\nPhone on the same Wi-Fi: your PC's IP, for example 192.168.1.20:5058")
                .setView(input)
                .setPositiveButton("Save", (dialog, which) -> {
                    try {
                        ApiBaseUrl.setOverride(input.getText().toString());
                        label.setText(serverLabel());
                        toast("Server updated. Sign in again.");
                    } catch (IllegalArgumentException error) {
                        toast(error.getMessage());
                    }
                })
                .setNeutralButton("Reset", (dialog, which) -> {
                    ApiBaseUrl.resetOverride();
                    label.setText(serverLabel());
                    toast("Using the build default.");
                })
                .setNegativeButton("Cancel", null)
                .show();
    }

    private void signIn() {
        if (busy) return;
        String email = emailInput.getText().toString().trim();
        String password = passwordInput.getText().toString();
        if (email.isEmpty() || password.isEmpty()) {
            showLoginMessage("Please enter your institutional email and password.");
            return;
        }
        busy = true;
        loginError.setVisibility(TextView.GONE);
        loginButton.setEnabled(false);
        loginButton.setText("Signing in...");
        io(() -> {
            LoginResponse user = LoginResponse.fromJson(ApiClient.post("api/Auth/login", new LoginRequest(email, password).toJson()));
            session.saveSession(user.token, user.fullName, user.email, user.userNumber,
                    android.text.TextUtils.join(",", user.roles), user.expiresAt, user.userId);
            runOnUiThread(this::openDashboard);
        });
    }

    private void showLoginMessage(String message) {
        Ui.confirmation(this, "Sign in required", message)
                .setPositiveButton("OK", null)
                .show();
    }

    @Override
    protected void onError(Exception error) {
        busy = false;
        if (loginButton != null) {
            loginButton.setEnabled(true);
            loginButton.setText("Sign In to Portal");
        }
        boolean invalidCredentials = error instanceof ApiClient.ApiException
                && ((ApiClient.ApiException) error).statusCode == 401;
        if (loginError != null) {
            String message = invalidCredentials
                    ? "Incorrect email or password. Please try again."
                    : (error.getMessage() == null || error.getMessage().isEmpty()
                    ? "We could not sign you in. Please try again." : error.getMessage());
            loginError.setText(message);
            loginError.setVisibility(TextView.VISIBLE);
        }
        // A 401 from the login endpoint means bad credentials, not an expired
        // session. Keep this screen visible so the user can read the message
        // and correct the form. Other failures use the shared error handling.
        if (!invalidCredentials) super.onError(error);
    }

    private void openDashboard() {
        Intent intent = new Intent(this, MainActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
        finish();
    }
}
