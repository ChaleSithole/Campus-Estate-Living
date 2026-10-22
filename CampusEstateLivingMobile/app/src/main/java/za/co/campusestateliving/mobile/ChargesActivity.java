package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

/** Rent and utility tariffs. Financial officers can review them. Only administrators can change amounts. */
public class ChargesActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!allowCategory(Roles.UI_FINANCE, Roles.UI_ADMIN)) return;
        bind(session.isFinanceUi() ? "Charges" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading charges..."));
        io(() -> {
            JSONObject charges = ApiClient.getObject("api/Charges", session.getToken());
            runOnUiThread(() -> show(charges));
        });
    }

    private void show(JSONObject charges) {
        root.removeAllViews();
        View screen = getLayoutInflater().inflate(R.layout.screen_section, root, true);
        boolean canEdit = charges.optBoolean("canEdit", false) && session.isAdministratorUi();
        ((TextView) screen.findViewById(R.id.sectionEyebrow)).setText("Rent and utilities");
        ((TextView) screen.findViewById(R.id.sectionTitle)).setText("Charges");
        ((TextView) screen.findViewById(R.id.sectionBody)).setText(canEdit
                ? "Update tariff amounts. This is limited to administrators, matching the estate office tools."
                : "Current tariffs. Amount changes stay with the administrator.");
        LinearLayout list = screen.findViewById(R.id.sectionList);
        TextView empty = screen.findViewById(R.id.sectionEmpty);
        JSONArray tariffs = charges.optJSONArray("tariffs");
        if (tariffs == null || tariffs.length() == 0) {
            empty.setVisibility(View.VISIBLE);
            empty.setText("No tariffs are configured.");
            return;
        }
        for (int i = 0; i < tariffs.length(); i++) {
            JSONObject tariff = tariffs.optJSONObject(i);
            if (tariff == null) continue;
            int id = tariff.optInt("tariffId");
            LinearLayout card = Ui.card(this);
            card.addView(Ui.text(this, tariff.optString("tariffName"), 16, Ui.ink(this)));
            card.addView(Ui.body(this, tariff.optString("categoryName")
                    + "  ·  " + Format.money(tariff.optDouble("amount", 0))
                    + " / " + tariff.optString("unit")
                    + (tariff.optBoolean("isActive", true) ? "" : "  ·  Inactive")));
            if (canEdit) {
                Button edit = Ui.button(this, "Update tariff", false);
                edit.setOnClickListener(v -> editTariff(id, tariff));
                card.addView(edit);
            }
            list.addView(card);
        }
    }

    private void editTariff(int id, JSONObject tariff) {
        EditText amount = Ui.field(this, "Amount", android.text.InputType.TYPE_CLASS_NUMBER | android.text.InputType.TYPE_NUMBER_FLAG_DECIMAL);
        amount.setText(String.valueOf(tariff.optDouble("amount", 0)));
        EditText unit = Ui.field(this, "Unit", android.text.InputType.TYPE_CLASS_TEXT);
        unit.setText(tariff.optString("unit", "Monthly"));
        LinearLayout form = new LinearLayout(this);
        form.setOrientation(LinearLayout.VERTICAL);
        form.addView(amount);
        form.addView(unit);
        Ui.confirmation(this, "Save charge settings?", "Apply these charge settings for Campus Estate Living accounts?")
                .setTitle(tariff.optString("tariffName"))
                .setView(form)
                .setPositiveButton("Save", (d, w) -> io(() -> {
                    JSONObject body = new JSONObject();
                    body.put("amount", Double.parseDouble(amount.getText().toString().trim()));
                    body.put("unit", unit.getText().toString().trim());
                    body.put("isActive", tariff.optBoolean("isActive", true));
                    ApiClient.put("api/Charges/tariffs/" + id, body, session.getToken());
                    runOnUiThread(() -> {
                        toast("Tariff updated.");
                        load();
                    });
                }))
                .setNegativeButton("Cancel", null)
                .show();
    }
}
