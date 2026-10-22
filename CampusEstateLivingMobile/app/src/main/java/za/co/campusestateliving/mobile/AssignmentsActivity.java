package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.graphics.drawable.GradientDrawable;
import android.text.InputType;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.HorizontalScrollView;
import android.widget.Spinner;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class AssignmentsActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!allowCategory(Roles.UI_ADMIN)) return;
        bind("Rooms");
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading rooms and assignments..."));
        io(() -> {
            JSONArray assignments = ApiClient.getArray("api/RoomAssignments", session.getToken());
            JSONArray rooms = ApiClient.getArray("api/Rooms", session.getToken());
            runOnUiThread(() -> show(assignments, rooms));
        });
    }

    private void show(JSONArray assignments, JSONArray rooms) {
        root.removeAllViews();
        View screen = getLayoutInflater().inflate(R.layout.screen_section, root, true);
        int occupied = 0;
        for (int i = 0; i < rooms.length(); i++) {
            JSONObject room = rooms.optJSONObject(i);
            if (room != null && room.optInt("currentOccupants", 0) > 0) occupied++;
        }
        ((TextView) screen.findViewById(R.id.sectionEyebrow)).setText("Room assignments");
        ((TextView) screen.findViewById(R.id.sectionTitle)).setText("Rooms");
        ((TextView) screen.findViewById(R.id.sectionBody)).setText(
                occupied + " of " + rooms.length() + " rooms have someone in residence.");
        Button action = screen.findViewById(R.id.sectionAction);
        action.setVisibility(View.GONE);
        LinearLayout list = screen.findViewById(R.id.sectionList);
        LinearLayout actions = new LinearLayout(this);
        actions.setOrientation(LinearLayout.HORIZONTAL);
        Button addRoom = Ui.button(this, "Add room", true);
        Button assignRoom = Ui.button(this, "Assign a room", false);
        LinearLayout.LayoutParams actionParams = new LinearLayout.LayoutParams(0, -2, 1f);
        actionParams.rightMargin = Ui.dp(this, 6);
        addRoom.setLayoutParams(actionParams);
        LinearLayout.LayoutParams assignParams = new LinearLayout.LayoutParams(0, -2, 1f);
        assignParams.leftMargin = Ui.dp(this, 6);
        assignRoom.setLayoutParams(assignParams);
        addRoom.setTextSize(13);
        assignRoom.setTextSize(13);
        addRoom.setOnClickListener(v -> createRoom());
        assignRoom.setOnClickListener(v -> open(AssignRoomActivity.class));
        actions.addView(addRoom);
        actions.addView(assignRoom);
        list.addView(actions);
        list.addView(Ui.eyebrow(this, "Room inventory"));
        if (rooms.length() == 0) {
            LinearLayout emptyRooms = Ui.card(this);
            emptyRooms.addView(Ui.text(this, "No rooms yet", 16, Ui.ink(this)));
            emptyRooms.addView(Ui.body(this, "Add a room to start organizing the estate inventory."));
            list.addView(emptyRooms);
        }
        Map<String, List<JSONObject>> roomsByBuilding = new LinkedHashMap<>();
        for (int i = 0; i < rooms.length(); i++) {
            JSONObject room = rooms.optJSONObject(i);
            if (room == null) continue;
            String building = room.optString("buildingName", "Unassigned building");
            if (building.isEmpty()) building = "Unassigned building";
            if (!roomsByBuilding.containsKey(building)) roomsByBuilding.put(building, new ArrayList<>());
            roomsByBuilding.get(building).add(room);
        }
        for (Map.Entry<String, List<JSONObject>> buildingEntry : roomsByBuilding.entrySet()) {
            List<JSONObject> buildingRooms = buildingEntry.getValue();
            int buildingOccupied = 0;
            for (JSONObject room : buildingRooms) {
                if (room.optInt("currentOccupants") > 0) buildingOccupied++;
            }
            LinearLayout buildingCard = Ui.card(this);
            LinearLayout buildingHeading = new LinearLayout(this);
            buildingHeading.setOrientation(LinearLayout.HORIZONTAL);
            buildingHeading.setGravity(android.view.Gravity.CENTER_VERTICAL);
            TextView buildingName = Ui.text(this, buildingEntry.getKey(), 17, Ui.ink(this));
            buildingName.setTypeface(Ui.sansBold());
            buildingHeading.addView(buildingName, new LinearLayout.LayoutParams(0, -2, 1f));
            TextView buildingCount = Ui.text(this, buildingRooms.size() + (buildingRooms.size() == 1 ? " room" : " rooms"), 12, Ui.muted(this));
            buildingHeading.addView(buildingCount);
            buildingCard.addView(buildingHeading);
            TextView buildingOccupancy = Ui.text(this, buildingOccupied + " occupied  ·  " + (buildingRooms.size() - buildingOccupied) + " available", 12, Ui.muted(this));
            buildingOccupancy.setPadding(0, Ui.dp(this, 3), 0, Ui.dp(this, 10));
            buildingCard.addView(buildingOccupancy);

            for (int roomIndex = 0; roomIndex < buildingRooms.size(); roomIndex += 2) {
                LinearLayout roomRow = new LinearLayout(this);
                roomRow.setOrientation(LinearLayout.HORIZONTAL);
                for (int column = 0; column < 2; column++) {
                    int index = roomIndex + column;
                    if (index >= buildingRooms.size()) {
                        roomRow.addView(new View(this), new LinearLayout.LayoutParams(0, Ui.dp(this, 1), 1f));
                        continue;
                    }
                    JSONObject room = buildingRooms.get(index);
                    LinearLayout tile = new LinearLayout(this);
                    tile.setOrientation(LinearLayout.VERTICAL);
                    int tilePad = Ui.dp(this, 10);
                    tile.setPadding(tilePad, tilePad, tilePad, tilePad);
                    GradientDrawable tileBackground = new GradientDrawable();
                    tileBackground.setColor(Ui.night(this) ? Ui.NAVY : 0xFFF4F6F9);
                    tileBackground.setCornerRadius(Ui.dp(this, 12));
                    tileBackground.setStroke(Ui.dp(this, 1), Ui.line(this));
                    tile.setBackground(tileBackground);
                    TextView roomName = Ui.text(this, "Room " + room.optString("roomNumber"), 14, Ui.ink(this));
                    roomName.setTypeface(Ui.sansBold());
                    tile.addView(roomName);
                    String roomType = room.optString("roomType");
                    if (roomType.isEmpty()) roomType = "Room";
                    TextView roomMeta = Ui.text(this, room.optInt("currentOccupants") + " / " + room.optInt("capacity") + " occupied  ·  " + roomType, 11, Ui.muted(this));
                    roomMeta.setPadding(0, Ui.dp(this, 3), 0, 0);
                    tile.addView(roomMeta);
                    LinearLayout.LayoutParams tileParams = new LinearLayout.LayoutParams(0, -2, 1f);
                    if (column == 0) tileParams.rightMargin = Ui.dp(this, 5);
                    else tileParams.leftMargin = Ui.dp(this, 5);
                    tileParams.bottomMargin = Ui.dp(this, 8);
                    roomRow.addView(tile, tileParams);
                }
                buildingCard.addView(roomRow);
            }
            list.addView(buildingCard);
        }
        list.addView(Ui.eyebrow(this, "Assignments"));
        TextView empty = screen.findViewById(R.id.sectionEmpty);
        int active = 0;
        for (int i = 0; i < assignments.length(); i++) {
            JSONObject row = assignments.optJSONObject(i);
            if (row == null || !row.optBoolean("isActive")) continue;
            active++;
            int id = row.optInt("assignmentId");
            LinearLayout card = Ui.card(this);
            card.addView(Ui.text(this, row.optString("studentName"), 16, Ui.ink(this)));
            card.addView(Ui.body(this, row.optString("buildingName") + " room " + row.optString("roomNumber")
                    + "  ·  from " + Format.when(row.optString("moveInDate"))));
            Button end = Ui.button(this, "End assignment", false);
            end.setOnClickListener(v -> confirmEnd(id, row.optString("studentName")));
            card.addView(end);
            list.addView(card);
        }
        if (active == 0) {
            empty.setVisibility(View.VISIBLE);
            empty.setText("No active room assignments.");
        }
    }

    private void confirmEnd(int id, String name) {
        Ui.confirmation(this, "End room assignment?", "This will end the resident's active room assignment in Campus Estate Living.")
                .setTitle("End assignment")
                .setMessage("Move " + name + " out of this room? This cannot be undone from the phone.")
                .setPositiveButton("End assignment", (d, w) -> io(() -> {
                    ApiClient.post("api/RoomAssignments/" + id + "/end", new JSONObject(), session.getToken());
                    runOnUiThread(() -> {
                        toast("Assignment ended.");
                        load();
                    });
                }))
                .setNegativeButton("Cancel", null)
                .show();
    }

    private void createRoom() {
        io(() -> {
            JSONArray properties = ApiClient.getArray("api/Properties", session.getToken());
            JSONArray types = ApiClient.getArray("api/Rooms/types", session.getToken());
            runOnUiThread(() -> promptRoom(properties, types));
        });
    }

    private void promptRoom(JSONArray properties, JSONArray types) {
        if (properties.length() == 0 || types.length() == 0) {
            toast("Add a building and room types before creating a room.");
            return;
        }
        String[] propertyLabels = new String[properties.length()];
        int[] propertyIds = new int[properties.length()];
        for (int i = 0; i < properties.length(); i++) {
            JSONObject property = properties.optJSONObject(i);
            propertyLabels[i] = property.optString("buildingName");
            propertyIds[i] = property.optInt("propertyId");
        }
        String[] typeLabels = new String[types.length()];
        int[] typeIds = new int[types.length()];
        for (int i = 0; i < types.length(); i++) {
            JSONObject type = types.optJSONObject(i);
            typeLabels[i] = type.optString("roomTypeName");
            typeIds[i] = type.optInt("roomTypeId");
        }
        Spinner propertySpinner = new Spinner(this);
        propertySpinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, propertyLabels));
        Spinner typeSpinner = new Spinner(this);
        typeSpinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, typeLabels));
        EditText number = Ui.field(this, "Room number", InputType.TYPE_CLASS_TEXT);
        LinearLayout form = new LinearLayout(this);
        form.setOrientation(LinearLayout.VERTICAL);
        form.setPadding(Ui.dp(this, 16), Ui.dp(this, 8), Ui.dp(this, 16), 0);
        form.addView(propertySpinner);
        form.addView(typeSpinner);
        form.addView(number);
        Ui.confirmation(this, "Add room?", "Add this room to your Campus Estate Living property register?")
                .setTitle("Add room")
                .setView(form)
                .setPositiveButton("Save", (d, w) -> io(() -> {
                    JSONObject body = new JSONObject();
                    body.put("propertyId", propertyIds[propertySpinner.getSelectedItemPosition()]);
                    body.put("roomTypeId", typeIds[typeSpinner.getSelectedItemPosition()]);
                    body.put("roomNumber", number.getText().toString().trim());
                    ApiClient.post("api/Rooms", body, session.getToken());
                    runOnUiThread(() -> {
                        toast("Room added.");
                        load();
                    });
                }))
                .setNegativeButton("Cancel", null)
                .show();
    }
}
