package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.text.InputType;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Spinner;

import org.json.JSONArray;
import org.json.JSONObject;

/** Officer-recorded EFT or cash. Amount must match the selected tariff. */
public class RecordPaymentActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(null);
        if (!session.isStaffSide()) {
            root.addView(Ui.body(this, "Only an administrator or financial officer can record a payment."));
            return;
        }
        int accountId = getIntent().getIntExtra("accountId", 0);
        String resident = getIntent().getStringExtra("residentName");
        root.addView(Ui.body(this, "Loading tariffs..."));
        io(() -> {
            JSONArray tariffs = ApiClient.getArray("api/Payments/tariffs", session.getToken());
            runOnUiThread(() -> show(accountId, resident, tariffs));
        });
    }

    private void show(int accountId, String resident, JSONArray tariffs) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Record payment"));
        root.addView(Ui.title(this, resident == null || resident.isEmpty() ? "Estate account " + accountId : resident));
        root.addView(Ui.body(this, "Cash and EFT recorded here are confirmed immediately. The amount is the tariff amount, which the API checks."));
        if (accountId <= 0) {
            root.addView(Ui.body(this, "Open a payment first. The users list does not include the estate account id required to record a payment."));
            return;
        }
        String[] labels = new String[tariffs.length()];
        int[] ids = new int[tariffs.length()];
        int[] categories = new int[tariffs.length()];
        double[] amounts = new double[tariffs.length()];
        for (int i = 0; i < tariffs.length(); i++) {
            JSONObject tariff = tariffs.optJSONObject(i);
            labels[i] = tariff.optString("categoryName") + " · " + tariff.optString("tariffName") + " · " + Format.money(tariff.optDouble("amount"));
            ids[i] = tariff.optInt("tariffId");
            categories[i] = tariff.optInt("categoryId");
            amounts[i] = tariff.optDouble("amount");
        }
        Spinner tariff = new Spinner(this);
        tariff.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, labels));
        Spinner method = new Spinner(this);
        method.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, new String[]{"EFT", "Cash"}));
        EditText notes = Ui.field(this, "Notes (optional)", InputType.TYPE_CLASS_TEXT);
        root.addView(tariff);
        root.addView(method);
        root.addView(notes);
        Button save = Ui.button(this, "Record payment", true);
        save.setOnClickListener(v -> {
            if (tariffs.length() == 0) {
                toast("No active tariffs were returned.");
                return;
            }
            int index = tariff.getSelectedItemPosition();
            String note = notes.getText().toString().trim();
            String chosenMethod = String.valueOf(method.getSelectedItem());
            Ui.confirmation(this, "Record this payment?", "This " + chosenMethod + " payment will be recorded and confirmed immediately for " + resident + ".")
                    .setNegativeButton("Review", null)
                    .setPositiveButton("Record payment", (dialog, which) -> io(() -> {
                JSONObject body = new JSONObject();
                body.put("accountId", accountId);
                body.put("tariffId", ids[index]);
                body.put("categoryId", categories[index]);
                body.put("amount", amounts[index]);
                body.put("paymentMethod", chosenMethod);
                if (!note.isEmpty()) body.put("notes", note);
                JSONObject created = ApiClient.post("api/Payments/record", body, session.getToken());
                runOnUiThread(() -> toast("Recorded " + created.optString("referenceNumber") + "."));
            })).show();
        });
        root.addView(save);
    }
}
