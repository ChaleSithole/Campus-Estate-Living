package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

/** Estate financial report. Not a personal balance. */
public class ReportsActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!allowCategory(Roles.UI_FINANCE, Roles.UI_ADMIN)) return;
        bind("Reports");
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading the financial report..."));
        io(() -> {
            JSONObject report = ApiClient.getObject("api/Reports", session.getToken());
            JSONObject export = null;
            try {
                export = ApiClient.getObject("api/Reports/export", session.getToken());
            } catch (Exception ignored) {
            }
            JSONObject file = export;
            runOnUiThread(() -> show(report, file));
        });
    }

    private void show(JSONObject report, JSONObject export) {
        root.removeAllViews();
        View screen = getLayoutInflater().inflate(R.layout.screen_section, root, true);
        ((TextView) screen.findViewById(R.id.sectionEyebrow)).setText(session.isAdministratorUi() ? "Administrative reports" : "Financial reports");
        ((TextView) screen.findViewById(R.id.sectionTitle)).setText("Reports");
        ((TextView) screen.findViewById(R.id.sectionBody)).setText(
                "Estate payment summary · " + Format.when(report.optString("from")) + " to " + Format.when(report.optString("to")));
        LinearLayout list = screen.findViewById(R.id.sectionList);
        JSONObject byCategory = report.optJSONObject("byCategory");
        JSONObject byStatus = report.optJSONObject("byStatus");
        LinearLayout metrics = new LinearLayout(this);
        metrics.setOrientation(LinearLayout.HORIZONTAL);
        metrics.addView(Ui.statTile(this, "Confirmed total", Format.money(report.optDouble("confirmedTotal", 0))));
        metrics.addView(Ui.statTile(this, "Payments", String.valueOf(report.optInt("paymentCount", 0))));
        list.addView(metrics);

        boolean hasCategoryRows = byCategory != null && byCategory.length() > 0;
        boolean hasStatusRows = byStatus != null && byStatus.length() > 0;
        if (hasCategoryRows) {
            LinearLayout categoryCard = Ui.card(this);
            TextView categoryTitle = Ui.text(this, "Confirmed by charge", 17, Ui.ink(this));
            categoryTitle.setTypeface(Ui.sansBold());
            categoryCard.addView(categoryTitle);
            TextView categoryCaption = Ui.text(this, "Confirmed payments grouped by charge category", 12, Ui.muted(this));
            categoryCaption.setPadding(0, Ui.dp(this, 3), 0, Ui.dp(this, 8));
            categoryCard.addView(categoryCaption);
            List<String> keys = new ArrayList<>();
            Iterator<String> iterator = byCategory.keys();
            while (iterator.hasNext()) keys.add(iterator.next());
            Collections.sort(keys, (left, right) -> Double.compare(byCategory.optDouble(right), byCategory.optDouble(left)));
            for (String key : keys) {
                categoryCard.addView(Ui.infoRow(this, categoryCard, key, Format.money(byCategory.optDouble(key, 0))));
            }
            list.addView(categoryCard);
        }
        if (hasStatusRows) {
            LinearLayout statusCard = Ui.card(this);
            TextView statusTitle = Ui.text(this, "Payments by status", 17, Ui.ink(this));
            statusTitle.setTypeface(Ui.sansBold());
            statusCard.addView(statusTitle);
            TextView statusCaption = Ui.text(this, "Payment count for each processing status", 12, Ui.muted(this));
            statusCaption.setPadding(0, Ui.dp(this, 3), 0, Ui.dp(this, 8));
            statusCard.addView(statusCaption);
            List<String> keys = new ArrayList<>();
            Iterator<String> iterator = byStatus.keys();
            while (iterator.hasNext()) keys.add(iterator.next());
            Collections.sort(keys);
            for (String key : keys) {
                statusCard.addView(Ui.infoRow(this, statusCard, key, String.valueOf(byStatus.optInt(key, 0))));
            }
            list.addView(statusCard);
        }
        if (!hasCategoryRows && !hasStatusRows) {
            TextView empty = screen.findViewById(R.id.sectionEmpty);
            empty.setVisibility(View.VISIBLE);
            empty.setText("No payments were found for this reporting period.");
        }
        if (export != null && export.optString("csv").length() > 0) {
            Button action = screen.findViewById(R.id.sectionAction);
            action.setVisibility(View.VISIBLE);
            action.setText("Copy report CSV");
            action.setOnClickListener(v -> {
                android.content.ClipboardManager clipboard = (android.content.ClipboardManager) getSystemService(CLIPBOARD_SERVICE);
                clipboard.setPrimaryClip(android.content.ClipData.newPlainText("CEL report", export.optString("csv")));
                toast("Report copied.");
            });
        }
    }
}
