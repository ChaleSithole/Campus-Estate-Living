package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.text.InputType;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

public class BuildingsActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!allowCategory(Roles.UI_ADMIN)) return;
        bind("Buildings");
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading buildings..."));
        io(() -> {
            JSONArray properties = ApiClient.getArray("api/Properties", session.getToken());
            runOnUiThread(() -> show(properties));
        });
    }

    private void show(JSONArray properties) {
        root.removeAllViews();
        View screen = getLayoutInflater().inflate(R.layout.screen_section, root, true);
        ((TextView) screen.findViewById(R.id.sectionEyebrow)).setText("Accommodation management");
        ((TextView) screen.findViewById(R.id.sectionTitle)).setText("Buildings");
        ((TextView) screen.findViewById(R.id.sectionBody)).setText("Estate buildings. The campus is configured for four buildings.");
        LinearLayout list = screen.findViewById(R.id.sectionList);
        Button action = screen.findViewById(R.id.sectionAction);
        action.setVisibility(View.GONE);
        LinearLayout actions = new LinearLayout(this);
        actions.setOrientation(LinearLayout.HORIZONTAL);
        Button addBuilding = Ui.button(this, "Add building", true);
        Button maintenance = Ui.button(this, "Maintenance", false);
        LinearLayout.LayoutParams addParams = new LinearLayout.LayoutParams(0, -2, 1f);
        addParams.rightMargin = Ui.dp(this, 6);
        addBuilding.setLayoutParams(addParams);
        LinearLayout.LayoutParams maintenanceParams = new LinearLayout.LayoutParams(0, -2, 1f);
        maintenanceParams.leftMargin = Ui.dp(this, 6);
        maintenance.setLayoutParams(maintenanceParams);
        addBuilding.setTextSize(13);
        maintenance.setTextSize(13);
        addBuilding.setOnClickListener(v -> createBuilding());
        maintenance.setOnClickListener(v -> open(MaintenanceActivity.class));
        actions.addView(addBuilding);
        actions.addView(maintenance);
        list.addView(actions);
        list.addView(Ui.eyebrow(this, "Estate buildings"));
        TextView empty = screen.findViewById(R.id.sectionEmpty);
        if (properties.length() == 0) {
            empty.setVisibility(View.VISIBLE);
            empty.setText("No buildings yet.");
            return;
        }
        for (int i = 0; i < properties.length(); i++) {
            JSONObject property = properties.optJSONObject(i);
            if (property == null) continue;
            list.addView(Ui.tappable(this,
                    property.optString("buildingName"),
                    property.optString("buildingCode") + "  ·  " + property.optInt("roomCount") + " rooms  ·  " + property.optString("propertyStatus"),
                    () -> open(AssignmentsActivity.class)));
        }
    }

    private void createBuilding() {
        EditText code = Ui.field(this, "Building code", InputType.TYPE_CLASS_TEXT);
        EditText name = Ui.field(this, "Building name", InputType.TYPE_CLASS_TEXT);
        EditText address = Ui.field(this, "Address", InputType.TYPE_CLASS_TEXT);
        LinearLayout form = new LinearLayout(this);
        form.setOrientation(LinearLayout.VERTICAL);
        form.setPadding(Ui.dp(this, 16), Ui.dp(this, 8), Ui.dp(this, 16), 0);
        form.addView(code);
        form.addView(name);
        form.addView(address);
        Ui.confirmation(this, "Save building?", "Add this building to your Campus Estate Living property register?")
                .setTitle("Add building")
                .setView(form)
                .setPositiveButton("Save", (d, w) -> io(() -> {
                    JSONObject body = new JSONObject();
                    body.put("buildingCode", code.getText().toString().trim());
                    body.put("buildingName", name.getText().toString().trim());
                    body.put("address", address.getText().toString().trim());
                    body.put("propertyStatus", "Active");
                    ApiClient.post("api/Properties", body, session.getToken());
                    runOnUiThread(() -> {
                        toast("Building added.");
                        load();
                    });
                }))
                .setNegativeButton("Cancel", null)
                .show();
    }
}
