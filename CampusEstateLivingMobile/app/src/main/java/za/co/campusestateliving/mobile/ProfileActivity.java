package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.app.AlertDialog;
import android.content.Intent;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.text.InputType;
import android.text.method.PasswordTransformationMethod;
import android.view.Gravity;
import android.widget.Button;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONObject;

public class ProfileActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(session.isAdministratorUi() ? "More" : "Profile");
        root.addView(Ui.body(this, "Loading profile details..."));
        load();
    }

    private void load() {
        io(() -> {
            JSONObject profile = ApiClient.getObject("api/Profile", session.getToken());
            runOnUiThread(() -> show(profile));
        });
    }

    private void show(JSONObject profile) {
        root.removeAllViews();
        View screen = getLayoutInflater().inflate(R.layout.screen_profile, root, true);

        String fullName = profile.optString("fullName", "User Profile");
        ((TextView) screen.findViewById(R.id.profileTitle)).setText(fullName);
        ((TextView) screen.findViewById(R.id.avatar)).setText(Ui.getInitials(profile.optString("fullName")));
        ((TextView) screen.findViewById(R.id.profileName)).setText(fullName);
        ((TextView) screen.findViewById(R.id.profileEmail)).setText(profile.optString("email"));
        ((FrameLayout) screen.findViewById(R.id.profileStatusSlot)).addView(
                Ui.status(this, profile.optBoolean("isActive", true) ? "Active" : "Suspended"));

        LinearLayout infoRows = screen.findViewById(R.id.infoRows);
        infoRows.removeAllViews();
        infoRows.addView(Ui.infoRow(this, infoRows, "Identification number", Format.text(profile.optString("userNumber"))));
        infoRows.addView(Ui.infoRow(this, infoRows, "Role", Format.roles(profile.optJSONArray("roles"))));
        if (session.isResidentUi()) {
            infoRows.addView(Ui.infoRow(this, infoRows, "Estate account", Format.text(profile.optString("accountNumber"))));
            infoRows.addView(Ui.infoRow(this, infoRows, "Account status", Format.text(profile.optString("accountStatus"))));
            String building = profile.optString("buildingName", "").trim();
            String room = profile.optString("roomNumber", "").trim();
            boolean hasBuilding = !building.isEmpty() && !"null".equalsIgnoreCase(building);
            boolean hasRoom = !room.isEmpty() && !"null".equalsIgnoreCase(room);
            String residence = hasBuilding
                    ? building + (hasRoom ? " \u00b7 Room " + room : "")
                    : "No room assigned yet";
            infoRows.addView(Ui.infoRow(this, infoRows, "Current residence", residence));
        }

        LinearLayout editCard = screen.findViewById(R.id.editCard);
        Button editToggle = Ui.button(this, "Edit personal information", false);
        editToggle.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_nav_people, 0, 0, 0);
        editToggle.setCompoundDrawablePadding(Ui.dp(this, 10));
        editToggle.setOnClickListener(v -> {
            boolean showEditor = editCard.getVisibility() != View.VISIBLE;
            editCard.setVisibility(showEditor ? View.VISIBLE : View.GONE);
            editToggle.setText(showEditor ? "Cancel editing" : "Edit personal information");
        });
        root.addView(editToggle);
        editCard.setVisibility(View.GONE);

        EditText first = screen.findViewById(R.id.firstNameInput);
        first.setText(profile.optString("firstName"));
        EditText last = screen.findViewById(R.id.lastNameInput);
        last.setText(profile.optString("lastName"));
        EditText phone = screen.findViewById(R.id.phoneInput);
        phone.setText(profile.optString("phoneNumber"));

        Button save = screen.findViewById(R.id.saveProfileButton);
        save.setOnClickListener(v -> {
            String given = first.getText().toString().trim();
            String family = last.getText().toString().trim();
            String number = phone.getText().toString().trim();
            if (given.isEmpty() || family.isEmpty()) {
                toast("First name and last name are required.");
                return;
            }
            Ui.confirmation(this, "Save profile changes?", "Your personal information will be updated on your Campus Estate Living account.")
                    .setNegativeButton("Review", null)
                    .setPositiveButton("Save changes", (dialog, which) -> io(() -> {
                        JSONObject body = new JSONObject();
                        body.put("firstName", given);
                        body.put("lastName", family);
                        body.put("phoneNumber", number);
                        ApiClient.put("api/Profile", body, session.getToken());
                        runOnUiThread(() -> {
                            toast("Profile updated successfully.");
                            load();
                        });
                    }))
                    .show();
        });

        root.addView(shortcuts());

        LinearLayout pwdCard = Ui.card(this);
        LinearLayout passwordFields = new LinearLayout(this);
        passwordFields.setOrientation(LinearLayout.VERTICAL);
        passwordFields.setVisibility(View.GONE);
        pwdCard.addView(Ui.text(this, "Security", 16, Ui.green(this)));
        Button passwordToggle = Ui.button(this, "Change password", false);
        passwordToggle.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_nav_card, 0, 0, 0);
        passwordToggle.setCompoundDrawablePadding(Ui.dp(this, 10));
        passwordToggle.setOnClickListener(v -> {
            boolean showFields = passwordFields.getVisibility() != View.VISIBLE;
            passwordFields.setVisibility(showFields ? View.VISIBLE : View.GONE);
            passwordToggle.setText(showFields ? "Cancel password change" : "Change password");
        });
        pwdCard.addView(passwordToggle);

        passwordFields.addView(Ui.text(this, "Current Password", 12, Ui.muted(this)));
        EditText current = secretField("Enter current password");
        passwordFields.addView(current);

        passwordFields.addView(Ui.text(this, "New Password", 12, Ui.muted(this)));
        EditText next = secretField("Enter new password");
        passwordFields.addView(next);

        passwordFields.addView(Ui.text(this, "Confirm New Password", 12, Ui.muted(this)));
        EditText again = secretField("Re-enter new password");
        passwordFields.addView(again);

        Button change = Ui.button(this, "Update security password", false);
        change.setOnClickListener(v -> {
            String currentPassword = current.getText().toString();
            String newPassword = next.getText().toString();
            String confirmPassword = again.getText().toString();
            if (currentPassword.isEmpty() || newPassword.isEmpty()) {
                toast("Please fill in both current and new passwords.");
                return;
            }
            if (!newPassword.equals(confirmPassword)) {
                toast("Passwords do not match.");
                return;
            }
            Ui.confirmation(this, "Update password?", "You will need the new password the next time you sign in to Campus Estate Living.")
                    .setNegativeButton("Review", null)
                    .setPositiveButton("Update password", (dialog, which) -> io(() -> {
                        JSONObject body = new JSONObject();
                        body.put("currentPassword", currentPassword);
                        body.put("newPassword", newPassword);
                        body.put("confirmPassword", confirmPassword);
                        ApiClient.post("api/Auth/change-password", body, session.getToken());
                        runOnUiThread(() -> {
                            current.setText("");
                            next.setText("");
                            again.setText("");
                            passwordFields.setVisibility(View.GONE);
                            passwordToggle.setText("Change password");
                            toast("Password updated successfully.");
                        });
                    }))
                    .show();
        });
        passwordFields.addView(change);
        pwdCard.addView(passwordFields);
        root.addView(pwdCard);

        Button signOut = Ui.button(this, "Sign out", false);
        signOut.setOnClickListener(v -> Ui.confirmation(this, "Sign out?", "You will leave your Campus Estate Living session on this device.")
                .setNegativeButton("Stay signed in", null)
                .setPositiveButton("Sign out", (dialog, which) -> {
            session.clearSession();
            Intent intent = new Intent(this, LoginActivity.class);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
            startActivity(intent);
            finish();
        }).show());
        root.addView(signOut);
    }

    private LinearLayout shortcuts() {
        LinearLayout block = new LinearLayout(this);
        block.setOrientation(LinearLayout.VERTICAL);
        block.addView(Ui.eyebrow(this, "Services"));
        block.addView(Ui.tappable(this, "Notifications", "Messages for this signed-in account", () -> open(NotificationsActivity.class)));
        block.addView(Ui.tappable(this, "Notices", "Estate announcements", () -> open(NoticesActivity.class)));
        block.addView(Ui.tappable(this, "Maintenance", session.isResidentUi() ? "Report a fault in your room" : "Review repair requests", () -> open(MaintenanceActivity.class)));
        if (session.isResidentUi()) {
            block.addView(Ui.tappable(this, "Cards and banks", "Payment methods on your account", () -> open(WalletActivity.class)));
            block.addView(Ui.tappable(this, "Vehicles", "Parking permits registered to you", () -> open(VehiclesActivity.class)));
        }
        if (session.isFinanceUi() || session.isAdministratorUi()) {
            block.addView(Ui.tappable(this, "Staff messages", "Private conversations with administrators and financial officers", () -> open(StaffMessagesActivity.class)));
            block.addView(Ui.tappable(this, "Charges and utilities", "Rent, water, electricity and other tariffs", () -> open(ChargesActivity.class)));
        }
        if (session.isAdministratorUi()) {
            block.addView(Ui.tappable(this, "Estate payment queue", "Operational payment queue. Not a personal balance.", () -> open(FinanceActivity.class)));
        }
        block.addView(Ui.tappable(this, "Reviews", session.isResidentUi() ? "Residence feedback" : "Resident reviews", () -> open(ReviewsActivity.class)));
        block.addView(Ui.tappable(this, "Appearance", "Theme: " + session.getTheme(), () -> {
            String mode = session.getTheme();
            String next = "light".equals(mode) ? "dark" : "dark".equals(mode) ? "system" : "light";
            session.setTheme(next);
            recreate();
        }));
        return block;
    }

    private EditText secretField(String hint) {
        EditText field = Ui.field(this, hint, InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        field.setTransformationMethod(PasswordTransformationMethod.getInstance());
        return field;
    }
}
