package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.app.AlertDialog;
import android.os.Bundle;
import android.widget.ArrayAdapter;
import android.widget.Spinner;

import org.json.JSONArray;
import org.json.JSONObject;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class AssignRoomActivity extends CelActivity {
    private int userId;
    private String fullName;
    private Spinner residentSpinner;
    private int[] residentIds;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        userId = getIntent().getIntExtra("userId", 0);
        fullName = getIntent().getStringExtra("fullName");
        bind(session.isAdministratorUi() ? "Rooms" : null);
        if (!session.hasRole(Roles.ADMINISTRATOR)) {
            root.addView(Ui.body(this, "Only an administrator can assign a room."));
            return;
        }
        if (userId > 0) loadProperties();
        else loadResidents();
    }

    private void loadResidents() {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Room assignments"));
        root.addView(Ui.title(this, "Assign a room"));
        root.addView(Ui.body(this, "Choose a resident, then select a building and an available room."));
        root.addView(Ui.body(this, "Loading residents..."));
        io(() -> {
            JSONArray users = ApiClient.getArray("api/Users?role=" + java.net.URLEncoder.encode(Roles.STUDENT, "UTF-8") + "&q=", session.getToken());
            runOnUiThread(() -> showResidents(users));
        });
    }

    private void showResidents(JSONArray users) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Room assignments"));
        root.addView(Ui.title(this, "Assign a room"));
        root.addView(Ui.body(this, "Choose a resident, then select a building and an available room."));
        String[] names = new String[users.length()];
        residentIds = new int[users.length()];
        for (int i = 0; i < users.length(); i++) {
            JSONObject user = users.optJSONObject(i);
            names[i] = user == null ? "Resident" : user.optString("fullName", "Resident");
            residentIds[i] = user == null ? 0 : user.optInt("userId");
        }
        if (users.length() == 0) {
            root.addView(Ui.body(this, "No residents are available to assign."));
            return;
        }
        residentSpinner = new Spinner(this);
        residentSpinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, names));
        residentSpinner.setBackgroundColor(Ui.cardColor(this));
        root.addView(residentSpinner);
        android.widget.Button next = Ui.button(this, "Choose a building", true);
        next.setOnClickListener(v -> {
            int selected = residentSpinner.getSelectedItemPosition();
            userId = residentIds[selected];
            fullName = names[selected];
            loadProperties();
        });
        root.addView(next);
    }

    private void loadProperties() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading buildings..."));
        io(() -> {
            JSONArray properties = ApiClient.getArray("api/Properties", session.getToken());
            runOnUiThread(() -> {
                root.removeAllViews();
                root.addView(Ui.eyebrow(this, "Assign room"));
                root.addView(Ui.title(this, fullName == null ? "Resident" : fullName));
                root.addView(Ui.body(this, "A resident with an active assignment cannot be assigned again. Sharing rooms keep a single gender. The API enforces both rules."));
                for (int i = 0; i < properties.length(); i++) {
                    JSONObject property = properties.optJSONObject(i);
                    if (property == null) continue;
                    int id = property.optInt("propertyId");
                    root.addView(Ui.tappable(this, property.optString("buildingName"), property.optString("buildingCode"),
                            () -> loadRooms(id, property.optString("buildingName"))));
                }
            });
        });
    }

    private void loadRooms(int propertyId, String building) {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading rooms..."));
        io(() -> {
            JSONArray rooms = ApiClient.getArray("api/Rooms/property/" + propertyId, session.getToken());
            runOnUiThread(() -> {
                root.removeAllViews();
                root.addView(Ui.title(this, building));
                int open = 0;
                for (int i = 0; i < rooms.length(); i++) {
                    JSONObject room = rooms.optJSONObject(i);
                    if (room == null || room.optInt("availableSpaces") <= 0) continue;
                    open++;
                    int roomId = room.optInt("roomId");
                    String label = "Room " + room.optString("roomNumber");
                    String detail = room.optString("roomType") + " · " + room.optInt("availableSpaces") + " open";
                    root.addView(Ui.tappable(this, label, detail, () -> confirm(roomId, label + " · " + detail)));
                }
                if (open == 0) root.addView(Ui.body(this, "No rooms in this building have open spaces."));
                android.widget.Button back = Ui.button(this, "Buildings", false);
                back.setOnClickListener(v -> loadProperties());
                root.addView(back);
            });
        });
    }

    private void confirm(int roomId, String label) {
        Ui.confirmation(this, "Assign room?", "Assign " + label + " to this resident in Campus Estate Living?")
                .setTitle("Assign this room?")
                .setMessage(label)
                .setNegativeButton("Cancel", null)
                .setPositiveButton("Assign", (dialog, which) -> io(() -> {
                    String today = new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date());
                    ApiClient.post("api/Rooms/" + roomId + "/assign/" + userId + "?moveInDate=" + today, new JSONObject(), session.getToken());
                    runOnUiThread(() -> {
                        toast("Resident assigned.");
                        finish();
                    });
                }))
                .show();
    }
}
