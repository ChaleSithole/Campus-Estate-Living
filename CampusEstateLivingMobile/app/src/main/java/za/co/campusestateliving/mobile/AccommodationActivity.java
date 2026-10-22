package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.os.Bundle;
import android.view.Gravity;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

public class AccommodationActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        if (!session.isResidentUi()) {
            bind(null);
            root.addView(Ui.title(this, "Not your accommodation"));
            root.addView(Ui.body(this, "This screen is your own room. Estate buildings and assignments are on the administrator dashboard."));
            return;
        }
        bind("Stay");
        root.addView(Ui.body(this, "Loading accommodation..."));
        loadMine();
    }

    private void loadMine() {
        io(() -> {
            JSONArray assignments = ApiClient.getArray("api/RoomAssignments/me", session.getToken());
            runOnUiThread(() -> showMine(assignments));
        });
    }

    private void showMine(JSONArray assignments) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Accommodation"));
        root.addView(Ui.title(this, "Your stay"));

        JSONObject current = null;
        for (int i = 0; i < assignments.length(); i++) {
            JSONObject row = assignments.optJSONObject(i);
            if (row != null && row.optBoolean("isActive")) current = row;
        }

        LinearLayout now = Ui.card(this);
        LinearLayout topBar = new LinearLayout(this);
        topBar.setOrientation(LinearLayout.HORIZONTAL);
        topBar.setGravity(Gravity.CENTER_VERTICAL);
        topBar.addView(Ui.eyebrow(this, "Digital Residence Key"), new LinearLayout.LayoutParams(0, -2, 1f));
        topBar.addView(Ui.status(this, current != null ? "Active" : "Unassigned"));
        now.addView(topBar);

        if (current == null) {
            now.addView(Ui.title(this, "No Active Room"));
            now.addView(Ui.body(this, "You do not have an active room assignment. The university campus estate office allocates rooms according to your registration."));
        } else {
            TextView bldg = Ui.text(this, current.optString("buildingName"), 18, Ui.green(this));
            bldg.setTypeface(Ui.serif());
            bldg.setPadding(0, Ui.dp(this, 6), 0, 0);
            now.addView(bldg);

            TextView roomNum = Ui.text(this, "Room " + current.optString("roomNumber"), 28, Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY);
            roomNum.setTypeface(Ui.serif());
            roomNum.setPadding(0, Ui.dp(this, 2), 0, Ui.dp(this, 8));
            now.addView(roomNum);

            LinearLayout specRow = new LinearLayout(this);
            specRow.setOrientation(LinearLayout.HORIZONTAL);
            Ui.pair(now, "Room Type", current.optString("roomType", "Standard"));
            Ui.pair(now, "Move-In Date", Format.when(current.optString("moveInDate")));

            Button faultBtn = Ui.button(this, "Report room maintenance", false);
            faultBtn.setOnClickListener(v -> open(MaintenanceActivity.class));
            now.addView(faultBtn);
        }
        root.addView(now);

        if (assignments.length() > 0) {
            root.addView(Ui.text(this, "Residence history", 18, Ui.green(this)));
            for (int i = 0; i < assignments.length(); i++) {
                JSONObject row = assignments.optJSONObject(i);
                if (row == null) continue;
                LinearLayout card = Ui.card(this);
                card.addView(Ui.text(this, row.optString("buildingName") + "  ·  Room " + row.optString("roomNumber"), 16, Ui.ink(this)));
                String dates = "Moved in: " + Format.when(row.optString("moveInDate"))
                        + (row.isNull("moveOutDate") ? "" : "  ·  Moved out: " + Format.when(row.optString("moveOutDate")));
                card.addView(Ui.body(this, row.optString("roomType") + "  ·  " + dates));
                card.addView(Ui.status(this, row.optBoolean("isActive") ? "Active" : "Ended"));
                root.addView(card);
            }
        }

        root.addView(Ui.tappable(this, "Browse campus buildings", "Explore estate residences, floor plans, and capacity.", this::loadProperties));
    }

    private void loadProperties() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading campus buildings..."));
        io(() -> {
            JSONArray properties = ApiClient.getArray("api/Properties", session.getToken());
            runOnUiThread(() -> {
                root.removeAllViews();
                root.addView(Ui.eyebrow(this, "Campus Estate"));
                root.addView(Ui.title(this, "Buildings & Halls"));
                if (properties.length() == 0) root.addView(Ui.body(this, "No buildings were returned by the estate server."));
                for (int i = 0; i < properties.length(); i++) {
                    JSONObject property = properties.optJSONObject(i);
                    if (property == null) continue;
                    int id = property.optInt("propertyId");
                    String title = property.optString("buildingName") + " (" + property.optString("buildingCode") + ")";
                    String detail = property.optString("propertyStatus", "Open") + "  ·  " + property.optInt("roomCount") + " total rooms";
                    root.addView(Ui.tappable(this, title, detail, () -> loadRooms(id, property.optString("buildingName"))));
                }
                Button back = Ui.button(this, "Back to my stay", false);
                back.setOnClickListener(v -> loadMine());
                root.addView(back);
            });
        });
    }

    private void loadRooms(int propertyId, String name) {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading rooms for " + name + "..."));
        io(() -> {
            JSONArray rooms = ApiClient.getArray("api/Rooms/property/" + propertyId, session.getToken());
            runOnUiThread(() -> {
                root.removeAllViews();
                root.addView(Ui.eyebrow(this, "Building residence"));
                root.addView(Ui.title(this, name));
                int shown = 0;
                for (int i = 0; i < rooms.length(); i++) {
                    JSONObject room = rooms.optJSONObject(i);
                    if (room == null) continue;
                    shown++;
                    LinearLayout card = Ui.card(this);
                    LinearLayout rTop = new LinearLayout(this);
                    rTop.setOrientation(LinearLayout.HORIZONTAL);
                    rTop.setGravity(Gravity.CENTER_VERTICAL);
                    TextView rNum = Ui.text(this, "Room " + room.optString("roomNumber"), 18, Ui.green(this));
                    rNum.setTypeface(Ui.serif());
                    rTop.addView(rNum, new LinearLayout.LayoutParams(0, -2, 1f));

                    int openSpaces = room.optInt("availableSpaces", 0);
                    rTop.addView(Ui.status(this, openSpaces > 0 ? openSpaces + " Open" : "Full"));
                    card.addView(rTop);

                    card.addView(Ui.body(this, room.optString("roomType") + "  ·  Occupancy: " +
                            room.optInt("currentOccupants") + " / " + room.optInt("capacity") + " beds occupied"));
                    root.addView(card);
                }
                if (shown == 0) root.addView(Ui.body(this, "No rooms were returned for this building."));
                Button back = Ui.button(this, "Back to buildings list", false);
                back.setOnClickListener(v -> loadProperties());
                root.addView(back);
            });
        });
    }
}
