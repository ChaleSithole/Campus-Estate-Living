package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.text.InputType;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;

import org.json.JSONArray;
import org.json.JSONObject;

/** Resident vehicles. Parking stays blocked by the API until two are registered. */
public class VehiclesActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(null);
        if (!session.isResidentUi()) {
            root.addView(Ui.body(this, "Only students and staff can register vehicles."));
            return;
        }
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading vehicles..."));
        io(() -> {
            JSONArray vehicles = ApiClient.getArray("api/Vehicles", session.getToken());
            runOnUiThread(() -> show(vehicles));
        });
    }

    private void show(JSONArray vehicles) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Parking"));
        root.addView(Ui.title(this, "Vehicles"));
        root.addView(Ui.body(this, "You can register up to three. One vehicle is included. Paid parking is available after the second is registered. That rule is checked by the API."));
        if (vehicles.length() == 0) root.addView(Ui.body(this, "No vehicles registered."));
        for (int i = 0; i < vehicles.length(); i++) {
            JSONObject vehicle = vehicles.optJSONObject(i);
            if (vehicle == null) continue;
            int id = vehicle.optInt("vehicleId");
            LinearLayout card = Ui.card(this);
            card.addView(Ui.text(this, vehicle.optString("registrationNumber"), 18, Ui.ink(this)));
            String make = vehicle.optString("makeAndModel", "");
            if (!make.isEmpty() && !"null".equals(make)) card.addView(Ui.body(this, make));
            Button remove = Ui.button(this, "Remove", false);
            remove.setOnClickListener(v -> io(() -> {
                ApiClient.delete("api/Vehicles/" + id, session.getToken());
                runOnUiThread(() -> {
                    toast("Vehicle removed.");
                    load();
                });
            }));
            card.addView(remove);
            root.addView(card);
        }
        if (vehicles.length() >= 3) return;
        EditText plate = Ui.field(this, "Registration number", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_CHARACTERS);
        EditText make = Ui.field(this, "Make and model (optional)", InputType.TYPE_CLASS_TEXT);
        root.addView(plate);
        root.addView(make);
        Button add = Ui.button(this, "Register vehicle", true);
        add.setOnClickListener(v -> {
            String registration = plate.getText().toString().trim();
            String model = make.getText().toString().trim();
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("registrationNumber", registration);
                if (!model.isEmpty()) body.put("makeAndModel", model);
                ApiClient.post("api/Vehicles", body, session.getToken());
                runOnUiThread(() -> {
                    toast("Vehicle registered.");
                    load();
                });
            });
        });
        root.addView(add);
    }
}
