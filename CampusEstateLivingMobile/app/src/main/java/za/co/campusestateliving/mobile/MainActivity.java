package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.Button;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.Iterator;

public class MainActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind("Home");
        root.addView(Ui.body(this, "Loading your dashboard..."));
        load();
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (session != null && session.isLoggedIn() && root != null && root.getChildCount() > 0) {
            // Refresh when returning so a new login cannot keep the previous role's cards.
        }
    }

    private void load() {
        io(() -> {
            JSONObject dash;
            try {
                dash = ApiClient.getObject("api/Dashboard", session.getToken());
            } catch (ApiClient.ApiException error) {
                if (error.statusCode == 401) throw error;
                dash = legacyDashboard();
            }
            JSONObject ready = dash;
            runOnUiThread(() -> render(ready));
        });
    }

    private void render(JSONObject dash) {
        root.removeAllViews();
        String apiCategory = dash.optString("uiCategory", "");
        if (!apiCategory.isEmpty() && !categoryMatches(apiCategory)) {
            root.addView(Ui.title(this, "Session mismatch"));
            root.addView(Ui.body(this, "The signed-in role does not match this dashboard. Sign in again so another account's data is not shown."));
            android.widget.Button again = Ui.button(this, "Sign out", true);
            again.setOnClickListener(v -> signOut());
            root.addView(again);
            return;
        }
        int category = session.uiCategory();
        Ui.updateNotificationCount(this, dash.optInt("unreadNotifications", 0));
        if (category == Roles.UI_ADMIN) showAdmin(dash);
        else if (category == Roles.UI_FINANCE) showFinance(dash);
        else showResident(dash);
    }

    private boolean categoryMatches(String apiCategory) {
        int category = session.uiCategory();
        if (category == Roles.UI_ADMIN) return "Administrator".equalsIgnoreCase(apiCategory);
        if (category == Roles.UI_FINANCE) return "FinancialOfficer".equalsIgnoreCase(apiCategory);
        return "Resident".equalsIgnoreCase(apiCategory);
    }

    private void showResident(JSONObject dash) {
        View home = getLayoutInflater().inflate(R.layout.screen_resident_home, root, true);
        String name = dash.optString("fullName", session.getFullName());
        ((TextView) home.findViewById(R.id.welcomeName)).setText(dash.optString("greeting", "Welcome") + ", " + firstName(name));
        ((TextView) home.findViewById(R.id.heroAmount)).setText(Format.money(dash.optDouble("outstandingBalance", 0)));
        String account = dash.optString("accountNumber", "");
        ((TextView) home.findViewById(R.id.heroCaption)).setText(
                name + (account.isEmpty() ? "" : "  ·  " + account));
        home.findViewById(R.id.inactiveCard).setVisibility(dash.optBoolean("isActive", true) ? View.GONE : View.VISIBLE);

        LinearLayout quick = home.findViewById(R.id.quickActions);
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_transact, "Pay", () -> open(PaymentsActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_card, "History", () -> open(HistoryActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_stay, "Stay", () -> open(AccommodationActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_quick_faults, "Faults", () -> open(MaintenanceActivity.class)));

        String building = dash.optString("buildingName", "");
        boolean hasRoom = building != null && !building.isEmpty() && !"null".equals(building);
        home.findViewById(R.id.roomEmpty).setVisibility(hasRoom ? View.GONE : View.VISIBLE);
        home.findViewById(R.id.roomFilled).setVisibility(hasRoom ? View.VISIBLE : View.GONE);
        if (hasRoom) {
            ((TextView) home.findViewById(R.id.roomBuilding)).setText(building);
            ((TextView) home.findViewById(R.id.roomNumber)).setText("Room " + dash.optString("roomNumber", ""));
            ((TextView) home.findViewById(R.id.roomStatus)).setText("Status: " + Format.text(dash.optString("accountStatus", "Active")));
        }
        home.findViewById(R.id.roomCard).setOnClickListener(v -> open(AccommodationActivity.class));

        ((TextView) home.findViewById(R.id.confirmedValue)).setText(Format.money(dash.optDouble("confirmedThisMonth", 0)));
        int unread = dash.optInt("unreadNotifications", 0);
        ((TextView) home.findViewById(R.id.unreadValue)).setText(String.valueOf(unread));

        LinearLayout charges = home.findViewById(R.id.chargesList);
        JSONArray chargeRows = dash.optJSONArray("charges");
        int chargeCount = 0;
        if (chargeRows != null) {
            for (int i = 0; i < chargeRows.length(); i++) {
                JSONObject row = chargeRows.optJSONObject(i);
                if (row == null) continue;
                String amount = row.isNull("amount") ? "" : "  ·  " + Format.money(row.optDouble("amount", 0));
                charges.addView(Ui.infoRow(this, charges, row.optString("categoryName"), row.optString("status") + amount));
                chargeCount++;
            }
        }
        home.findViewById(R.id.chargesEmpty).setVisibility(chargeCount == 0 ? View.VISIBLE : View.GONE);

        fillPayments(home.findViewById(R.id.recentList), home.findViewById(R.id.recentEmpty), dash.optJSONArray("recentPayments"), true);
        home.findViewById(R.id.viewAll).setOnClickListener(v -> open(HistoryActivity.class));
        fillAlert(home.findViewById(R.id.alertsContainer), dash.optInt("unreadNotifications", 0));
    }

    private void showFinance(JSONObject dash) {
        View home = getLayoutInflater().inflate(R.layout.screen_finance_home, root, true);
        String name = dash.optString("fullName", session.getFullName());
        ((TextView) home.findViewById(R.id.welcomeName)).setText(dash.optString("greeting", "Welcome") + ", " + firstName(name));
        ((TextView) home.findViewById(R.id.heroAmount)).setText(Format.money(dash.optDouble("confirmedThisMonth", 0)));
        ((TextView) home.findViewById(R.id.heroCaption)).setText("Confirmed this month  ·  " + name);

        LinearLayout quick = home.findViewById(R.id.quickActions);
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_transact, "Transactions", () -> open(FinanceActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_card, "Charges", () -> open(ChargesActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_people, "Accounts", () -> open(UsersActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_explore, "Reports", () -> open(ReportsActivity.class)));

        ((TextView) home.findViewById(R.id.pendingAmount)).setText(Format.money(dash.optDouble("pendingAmount", 0)));
        ((TextView) home.findViewById(R.id.pendingCount)).setText(dash.optInt("pendingCount", 0) + " pending");
        ((TextView) home.findViewById(R.id.failedCount)).setText(String.valueOf(dash.optInt("failedCount", 0)) + " failed");
        ((TextView) home.findViewById(R.id.confirmedToday)).setText(dash.optInt("confirmedToday", 0) + " confirmed today");

        LinearLayout categories = home.findViewById(R.id.categoryList);
        JSONObject totals = dash.optJSONObject("totalsByCategory");
        int shown = 0;
        if (totals != null) {
            Iterator<String> keys = totals.keys();
            while (keys.hasNext()) {
                String key = keys.next();
                categories.addView(Ui.infoRow(this, categories, key, Format.money(totals.optDouble(key, 0))));
                shown++;
            }
        }
        home.findViewById(R.id.categoryEmpty).setVisibility(shown == 0 ? View.VISIBLE : View.GONE);
        fillPayments(home.findViewById(R.id.recentList), home.findViewById(R.id.recentEmpty), dash.optJSONArray("recentPayments"), false);
        home.findViewById(R.id.viewAll).setOnClickListener(v -> open(FinanceActivity.class));
        fillAlert(home.findViewById(R.id.alertsContainer), dash.optInt("unreadNotifications", 0));
    }

    private void showAdmin(JSONObject dash) {
        View home = getLayoutInflater().inflate(R.layout.screen_admin_home, root, true);
        String name = dash.optString("fullName", session.getFullName());
        int occupied = dash.optInt("occupiedRooms", 0);
        int rooms = dash.optInt("activeRooms", 0);
        ((TextView) home.findViewById(R.id.welcomeName)).setText(dash.optString("greeting", "Welcome") + ", " + firstName(name));
        ((TextView) home.findViewById(R.id.heroAmount)).setText(occupied + " / " + rooms);
        ((TextView) home.findViewById(R.id.heroCaption)).setText("Rooms occupied  ·  " + name);

        LinearLayout quick = home.findViewById(R.id.quickActions);
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_people, "Users", () -> open(UsersActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_stay, "Buildings", () -> open(BuildingsActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_nav_explore, "Rooms", () -> open(AssignmentsActivity.class)));
        quick.addView(Ui.quickAction(this, R.drawable.ic_quick_faults, "Repairs", () -> open(MaintenanceActivity.class)));

        ((TextView) home.findViewById(R.id.residentCount)).setText(String.valueOf(dash.optInt("residentCount", 0)));
        ((TextView) home.findViewById(R.id.officerCount)).setText(String.valueOf(dash.optInt("officerCount", 0)));
        ((TextView) home.findViewById(R.id.occupiedValue)).setText(occupied + " / " + rooms);
        ((TextView) home.findViewById(R.id.maintenanceValue)).setText(String.valueOf(dash.optInt("openMaintenance", 0)));
        ((TextView) home.findViewById(R.id.receivedValue)).setText(Format.money(dash.optDouble("confirmedThisMonth", 0)));
        ((TextView) home.findViewById(R.id.pendingEstateValue)).setText(
                dash.optInt("pendingCount", 0) + "  ·  " + Format.money(dash.optDouble("pendingAmount", 0)));
        home.findViewById(R.id.maintenanceValue).setOnClickListener(v -> open(MaintenanceActivity.class));

        int openMaintenance = dash.optInt("openMaintenance", 0);
        int unoccupiedRooms = Math.max(0, rooms - occupied);
        TextView operationsSummary = home.findViewById(R.id.operationsSummary);
        operationsSummary.setText(openMaintenance + (openMaintenance == 1 ? " maintenance request needs" : " maintenance requests need")
                + " review · " + unoccupiedRooms + (unoccupiedRooms == 1 ? " room has" : " rooms have") + " no current occupants.");
        LinearLayout operations = home.findViewById(R.id.operationsActions);
        Button maintenanceAction = Ui.button(this, "Review maintenance", false);
        maintenanceAction.setOnClickListener(v -> open(MaintenanceActivity.class));
        operations.addView(maintenanceAction);
        Button roomAction = Ui.button(this, "Manage rooms and assignments", true);
        roomAction.setOnClickListener(v -> open(AssignmentsActivity.class));
        operations.addView(roomAction);
        fillAlert(home.findViewById(R.id.alertsContainer), dash.optInt("unreadNotifications", 0));
    }

    private void fillPayments(LinearLayout list, TextView empty, JSONArray payments, boolean own) {
        list.removeAllViews();
        int shown = 0;
        if (payments != null) {
            for (int i = 0; i < payments.length() && shown < 5; i++) {
                JSONObject payment = payments.optJSONObject(i);
                if (payment == null) continue;
                int id = payment.optInt("paymentId");
                String title = own ? payment.optString("categoryName", "Payment")
                        : payment.optString("residentName", "Resident") + "  ·  " + payment.optString("categoryName", "");
                JSONObject row = payment;
                if (!own) {
                    try {
                        row = new JSONObject(payment.toString());
                        row.put("categoryName", title);
                    } catch (Exception ignored) {
                        row = payment;
                    }
                }
                list.addView(Ui.paymentRow(this, list, row, () -> openDetail(id)));
                shown++;
            }
        }
        empty.setVisibility(shown == 0 ? View.VISIBLE : View.GONE);
    }

    private void fillAlert(LinearLayout alerts, int unread) {
        alerts.removeAllViews();
        if (unread > 0) {
            alerts.addView(Ui.tappable(this, "Notifications",
                    unread + " unread message" + (unread == 1 ? "" : "s"),
                    () -> open(NotificationsActivity.class)));
        }
    }

    private void openDetail(int id) {
        Intent intent = new Intent(this, PaymentDetailActivity.class);
        intent.putExtra("paymentId", id);
        startActivity(intent);
    }

    private void signOut() {
        Ui.confirmation(this, "Sign out?", "You will leave your Campus Estate Living session on this device.")
                .setNegativeButton("Stay signed in", null)
                .setPositiveButton("Sign out", (dialog, which) -> completeSignOut())
                .show();
    }

    private void completeSignOut() {
        session.clearSession();
        Intent intent = new Intent(this, LoginActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
        finish();
    }

    private static String firstName(String name) {
        if (name == null || name.trim().isEmpty()) return "there";
        return name.trim().split("\\s+")[0];
    }

    private JSONObject legacyDashboard() throws Exception {
        String token = session.getToken();
        JSONObject dash = new JSONObject();
        int category = session.uiCategory();
        dash.put("uiCategory", category == Roles.UI_ADMIN ? "Administrator" : category == Roles.UI_FINANCE ? "FinancialOfficer" : "Resident");
        dash.put("fullName", session.getFullName());
        dash.put("greeting", "Welcome");
        dash.put("isActive", true);
        try {
            dash.put("unreadNotifications", ApiClient.getObject("api/Notifications/unread-count", token).optInt("count", 0));
        } catch (Exception ignored) {
            dash.put("unreadNotifications", 0);
        }
        if (category == Roles.UI_RESIDENT) fillLegacyResident(dash, token);
        else if (category == Roles.UI_FINANCE) fillLegacyFinance(dash, token);
        else fillLegacyAdmin(dash, token);
        return dash;
    }

    private void fillLegacyResident(JSONObject dash, String token) throws Exception {
        JSONObject profile = ApiClient.getObject("api/Profile", token);
        dash.put("fullName", profile.optString("fullName", session.getFullName()));
        dash.put("isActive", profile.optBoolean("isActive", true));
        dash.put("accountNumber", profile.optString("accountNumber", ""));
        dash.put("accountStatus", profile.optString("accountStatus", ""));
        dash.put("buildingName", profile.optString("buildingName", ""));
        dash.put("roomNumber", profile.optString("roomNumber", ""));
        JSONArray payments = new JSONArray();
        try {
            payments = ApiClient.getArray("api/Payments", token);
        } catch (Exception ignored) {
        }
        double pending = 0;
        JSONArray recent = new JSONArray();
        for (int i = 0; i < payments.length(); i++) {
            JSONObject payment = payments.optJSONObject(i);
            if (payment == null) continue;
            if ("Pending".equalsIgnoreCase(payment.optString("status"))) pending += payment.optDouble("amount", 0);
            if (recent.length() < 5) recent.put(payment);
        }
        dash.put("outstandingBalance", pending);
        dash.put("recentPayments", recent);
        dash.put("charges", new JSONArray());
    }

    private void fillLegacyFinance(JSONObject dash, String token) throws Exception {
        JSONArray queue = new JSONArray();
        try {
            queue = ApiClient.getArray("api/Payments/all?status=Pending", token);
        } catch (Exception ignored) {
        }
        double pending = 0;
        JSONArray recent = new JSONArray();
        for (int i = 0; i < queue.length(); i++) {
            JSONObject payment = queue.optJSONObject(i);
            if (payment == null) continue;
            pending += payment.optDouble("amount", 0);
            if (recent.length() < 8) recent.put(payment);
        }
        dash.put("pendingCount", queue.length());
        dash.put("pendingAmount", pending);
        dash.put("recentPayments", recent);
        try {
            JSONObject report = ApiClient.getObject("api/Reports", token);
            dash.put("confirmedThisMonth", report.optDouble("confirmedTotal", 0));
            dash.put("totalsByCategory", report.optJSONObject("byCategory"));
            JSONObject byStatus = report.optJSONObject("byStatus");
            dash.put("failedCount", byStatus == null ? 0 : byStatus.optInt("Failed", 0));
        } catch (Exception ignored) {
        }
    }

    private void fillLegacyAdmin(JSONObject dash, String token) throws Exception {
        try {
            JSONObject report = ApiClient.getObject("api/Reports", token);
            dash.put("confirmedThisMonth", report.optDouble("confirmedTotal", 0));
            JSONObject byStatus = report.optJSONObject("byStatus");
            dash.put("pendingCount", byStatus == null ? 0 : byStatus.optInt("Pending", 0));
        } catch (Exception ignored) {
        }
        try {
            JSONArray rooms = ApiClient.getArray("api/Rooms", token);
            int occupiedRooms = 0;
            for (int i = 0; i < rooms.length(); i++) {
                JSONObject room = rooms.optJSONObject(i);
                if (room != null && room.optInt("currentOccupants", 0) > 0) occupiedRooms++;
            }
            dash.put("activeRooms", rooms.length());
            dash.put("occupiedRooms", occupiedRooms);
        } catch (Exception ignored) {
        }
        try {
            JSONArray users = ApiClient.getArray("api/Users", token);
            int residents = 0;
            int officers = 0;
            JSONArray recent = new JSONArray();
            for (int i = 0; i < users.length(); i++) {
                JSONObject user = users.optJSONObject(i);
                if (user == null) continue;
                if (!user.optString("accountNumber", "").isEmpty()) residents++;
                if (Format.roles(user.optJSONArray("roles")).contains("FinancialOfficer")) officers++;
                if (recent.length() < 6) recent.put(user);
            }
            dash.put("residentCount", residents);
            dash.put("officerCount", officers);
            dash.put("recentUsers", recent);
        } catch (Exception ignored) {
        }
    }
}
