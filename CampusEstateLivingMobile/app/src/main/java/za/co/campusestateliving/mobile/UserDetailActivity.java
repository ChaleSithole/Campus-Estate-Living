package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.os.Bundle;
import android.text.InputType;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;

import org.json.JSONObject;

public class UserDetailActivity extends CelActivity {
    private int userId;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        userId = getIntent().getIntExtra("userId", 0);
        bind(session.isAdministratorUi() ? "Users" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading person..."));
        io(() -> {
            JSONObject user = ApiClient.getObject("api/Users/" + userId, session.getToken());
            runOnUiThread(() -> show(user));
        });
    }

    private void show(JSONObject user) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, user.optString("email")));
        root.addView(Ui.title(this, user.optString("fullName")));
        LinearLayout card = Ui.card(this);
        Ui.pair(card, "Number", user.optString("userNumber"));
        Ui.pair(card, "Roles", Format.roles(user.optJSONArray("roles")));
        Ui.pair(card, "Gender", Format.text(user.optString("gender")));
        Ui.pair(card, "Date of birth", Format.when(user.optString("dateOfBirth")));
        Ui.pair(card, "Estate account", Format.text(user.optString("accountNumber")));
        Ui.pair(card, "Account status", Format.text(user.optString("accountStatus")));
        card.addView(Ui.status(this, user.optBoolean("isActive") ? "Active" : "Inactive"));
        root.addView(card);

        int accountId = user.optInt("accountId", 0);
        if (session.isStaffSide() && accountId > 0) {
            Button record = Ui.button(this, "Record a payment", true);
            record.setOnClickListener(v -> {
                Intent intent = new Intent(this, RecordPaymentActivity.class);
                intent.putExtra("accountId", accountId);
                intent.putExtra("residentName", user.optString("fullName"));
                startActivity(intent);
            });
            root.addView(record);
        }

        String roles = Format.roles(user.optJSONArray("roles"));
        boolean officer = Roles.has(roles, Roles.FINANCIAL_OFFICER);
        boolean resident = Roles.has(roles, Roles.STUDENT) || Roles.has(roles, Roles.STAFF);
        if (session.hasRole(Roles.ADMINISTRATOR) && resident) {
            Button assign = Ui.button(this, "Assign a room", true);
            assign.setOnClickListener(v -> {
                Intent intent = new Intent(this, AssignRoomActivity.class);
                intent.putExtra("userId", userId);
                intent.putExtra("fullName", user.optString("fullName"));
                startActivity(intent);
            });
            root.addView(assign);
        }
        if (session.hasRole(Roles.ADMINISTRATOR) && officer) {
            boolean active = user.optBoolean("isActive");
            Button auth = Ui.button(this, active ? "Revoke officer access" : "Authorise officer", false);
            auth.setOnClickListener(v -> Ui.confirmation(this, active ? "Revoke officer approval?" : "Approve financial officer?", active ? "This officer will lose financial officer access in Campus Estate Living." : "This account will be approved for financial officer access.")
                    .setNegativeButton("Review", null)
                    .setPositiveButton(active ? "Revoke approval" : "Approve", (dialog, which) -> io(() -> {
                JSONObject body = new JSONObject();
                body.put("isActive", !active);
                ApiClient.post("api/Users/" + userId + "/authorise", body, session.getToken());
                runOnUiThread(() -> {
                    toast(active ? "Financial officer deactivated." : "Financial officer authorised.");
                    load();
                });
            })).show());
            root.addView(auth);
        }
        if (session.hasRole(Roles.ADMINISTRATOR) && user.optBoolean("isActive")) {
            Button deactivate = Ui.button(this, "Deactivate account", false);
            deactivate.setOnClickListener(v -> Ui.confirmation(this, "Deactivate account?", "This user will no longer be able to access Campus Estate Living.")
                    .setNegativeButton("Keep account", null)
                    .setPositiveButton("Deactivate", (dialog, which) -> io(() -> {
                ApiClient.post("api/Users/" + userId + "/deactivate", new JSONObject(), session.getToken());
                runOnUiThread(() -> {
                    toast("User deactivated.");
                    load();
                });
            })).show());
            root.addView(deactivate);
        }
        root.addView(Ui.text(this, "Reset password", 18, Ui.green(this)));
        EditText password = Ui.field(this, "New password", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        root.addView(password);
        Button reset = Ui.button(this, "Reset password", false);
        reset.setOnClickListener(v -> {
            String newPassword = password.getText().toString();
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("newPassword", newPassword);
                ApiClient.post("api/Users/" + userId + "/reset-password", body, session.getToken());
                runOnUiThread(() -> {
                    password.setText("");
                    toast("Password reset.");
                });
            });
        });
        root.addView(reset);
        Button back = Ui.button(this, "Back", false);
        back.setOnClickListener(v -> finish());
        root.addView(back);
    }
}
