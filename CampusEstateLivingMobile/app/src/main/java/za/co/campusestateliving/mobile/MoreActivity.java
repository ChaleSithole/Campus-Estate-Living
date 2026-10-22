package za.co.campusestateliving.mobile;

import android.content.Intent;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.view.Gravity;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

public class MoreActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(session.isAdministratorUi() ? "More" : "Explore");

        root.addView(Ui.title(this, "Explore"));

        // User Profile Summary Card
        LinearLayout userCard = Ui.card(this);
        LinearLayout userRow = new LinearLayout(this);
        userRow.setOrientation(LinearLayout.HORIZONTAL);
        userRow.setGravity(Gravity.CENTER_VERTICAL);

        // Avatar
        TextView avatar = new TextView(this);
        avatar.setText(Ui.getInitials(session.getFullName()));
        avatar.setTextSize(18);
        avatar.setTypeface(Ui.sansBold());
        avatar.setTextColor(0xFF051321);
        avatar.setGravity(Gravity.CENTER);
        int avSize = Ui.dp(this, 50);
        GradientDrawable avD = new GradientDrawable();
        avD.setShape(GradientDrawable.OVAL);
        avD.setOrientation(GradientDrawable.Orientation.TL_BR);
        avD.setColors(new int[]{0xFFE8CA7C, 0xFFC4A35A});
        avatar.setBackground(avD);
        LinearLayout.LayoutParams avP = new LinearLayout.LayoutParams(avSize, avSize);
        avP.setMargins(0, 0, Ui.dp(this, 14), 0);
        avatar.setLayoutParams(avP);
        userRow.addView(avatar);

        LinearLayout infoCol = new LinearLayout(this);
        infoCol.setOrientation(LinearLayout.VERTICAL);
        TextView nameV = Ui.text(this, session.getFullName().isEmpty() ? "Campus Resident" : session.getFullName(), 18, Ui.green(this));
        nameV.setTypeface(Ui.serif());
        infoCol.addView(nameV);

        String roleStr = session.isAdministratorUi() ? "Estate administrator"
                : session.isFinanceUi() ? "Financial officer"
                : session.hasRole(Roles.STAFF) ? "Staff resident" : "Student resident";
        TextView roleV = Ui.text(this, roleStr, 13, Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY);
        roleV.setPadding(0, Ui.dp(this, 2), 0, 0);
        infoCol.addView(roleV);

        userRow.addView(infoCol, new LinearLayout.LayoutParams(0, -2, 1f));
        userCard.addView(userRow);

        userCard.setClickable(true);
        userCard.setFocusable(true);
        userCard.setOnClickListener(v -> open(ProfileActivity.class));
        root.addView(userCard);

        // Account & Preferences Section
        root.addView(Ui.eyebrow(this, "Account"));
        root.addView(Ui.tappable(this, "Profile Information", "Update name, contact numbers, and security credentials", () -> open(ProfileActivity.class)));
        root.addView(Ui.tappable(this, "Theme & Appearance", "Active mode: " + session.getTheme().toUpperCase(), this::cycleTheme));

        // Estate Services Section
        root.addView(Ui.eyebrow(this, "Estate Services"));
        root.addView(Ui.tappable(this, "Campus Notices & Bulletin", "Official estate updates, campus circulars, and announcements", () -> open(NoticesActivity.class)));
        root.addView(Ui.tappable(this, "Maintenance & Repairs", session.isStaffSide() ? "Review facility repair tickets" : "Log room or estate repair requests", () -> open(MaintenanceActivity.class)));

        if (session.isResident()) {
            root.addView(Ui.tappable(this, "Digital Wallet & Cards", "Manage saved cards and bank accounts", () -> open(WalletActivity.class)));
            root.addView(Ui.tappable(this, "Vehicle Parking Permits", "Manage registered vehicles and permits", () -> open(VehiclesActivity.class)));
            root.addView(Ui.tappable(this, "Residence Reviews", "Submit and view residence quality feedback", () -> open(ReviewsActivity.class)));
        }

        if (session.isStaffSide()) {
            root.addView(Ui.eyebrow(this, "Administration"));
            root.addView(Ui.tappable(this, "Staff messages", "Private conversations with administrators and financial officers", () -> open(StaffMessagesActivity.class)));
            if (session.isAdministratorUi()) {
                root.addView(Ui.tappable(this, "Buildings", "Manage estate buildings and view room totals", () -> open(BuildingsActivity.class)));
                root.addView(Ui.tappable(this, "Rooms & assignments", "Add rooms, assign residents, and review occupancy", () -> open(AssignmentsActivity.class)));
                root.addView(Ui.tappable(this, "Maintenance queue", "Review open repair requests and update their status", () -> open(MaintenanceActivity.class)));
                root.addView(Ui.tappable(this, "Financial reports", "Review estate payment and charge summaries", () -> open(ReportsActivity.class)));
                root.addView(Ui.tappable(this, "Notices", "Publish and manage estate announcements", () -> open(NoticesActivity.class)));
                root.addView(Ui.tappable(this, "Notifications", "Review resident and estate notifications", () -> open(NotificationsActivity.class)));
            }
            root.addView(Ui.tappable(this, "Finance Management", "Process pending EFT verification queue and records", () -> open(FinanceActivity.class)));
            root.addView(Ui.tappable(this, "Resident Directory", session.hasRole(Roles.ADMINISTRATOR) ? "Manage user profiles, accounts, and estate roles" : "Directory of residents and accounts", () -> open(UsersActivity.class)));
            root.addView(Ui.tappable(this, "Campus Reviews & Ratings", "Read reviews and satisfaction ratings submitted by students", () -> open(ReviewsActivity.class)));
        }

        Button signOut = Ui.button(this, "Sign out of session", false);
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

    private void cycleTheme() {
        String mode = session.getTheme();
        String next = "light".equals(mode) ? "dark" : "dark".equals(mode) ? "system" : "light";
        session.setTheme(next);
        recreate();
    }
}
