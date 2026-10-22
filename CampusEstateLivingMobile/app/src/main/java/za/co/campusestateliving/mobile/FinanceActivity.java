package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.graphics.drawable.GradientDrawable;
import android.view.Gravity;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.Iterator;

public class FinanceActivity extends CelActivity {
    private String statusFilter = "Pending";

    @Override
    protected void onResume() {
        super.onResume();
        if (!session.isLoggedIn()) return;
        if (!session.isStaffSide()) {
            toast("Finance tools are limited to administrators and financial officers.");
            finish();
            return;
        }
        if (root == null) bind(session.isFinanceUi() ? "Transactions" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading payment queue and financial statements..."));
        String path = statusFilter.isEmpty() ? "api/Payments/all" : "api/Payments/all?status=" + statusFilter;
        io(() -> {
            JSONArray payments = ApiClient.getArray(path, session.getToken());
            JSONObject report = null;
            String reportError = null;
            try {
                report = ApiClient.getObject("api/Reports", session.getToken());
            } catch (Exception error) {
                reportError = error.getMessage();
            }
            JSONObject finalReport = report;
            String finalError = reportError;
            runOnUiThread(() -> show(payments, finalReport, finalError));
        });
    }

    private void show(JSONArray payments, JSONObject report, String reportError) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, session.isFinanceUi() ? "Financial operations" : "Estate payments"));
        root.addView(Ui.title(this, session.isFinanceUi() ? "Transactions" : "Payment queue"));

        if (report != null) {
            LinearLayout statRow = new LinearLayout(this);
            statRow.setOrientation(LinearLayout.HORIZONTAL);
            statRow.setPadding(0, 0, 0, Ui.dp(this, 12));

            LinearLayout t1 = Ui.statTile(this, "Confirmed Revenue", Format.money(report.optDouble("confirmedTotal", 0)));
            LinearLayout t2 = Ui.statTile(this, "Total Payments", String.valueOf(report.optInt("paymentCount", 0)));

            LinearLayout.LayoutParams p1 = new LinearLayout.LayoutParams(0, -2, 1f);
            p1.setMargins(0, 0, Ui.dp(this, 6), 0);
            t1.setLayoutParams(p1);

            LinearLayout.LayoutParams p2 = new LinearLayout.LayoutParams(0, -2, 1f);
            p2.setMargins(Ui.dp(this, 6), 0, 0, 0);
            t2.setLayoutParams(p2);

            statRow.addView(t1);
            statRow.addView(t2);
            root.addView(statRow);
        }

        // Horizontal filter tabs
        LinearLayout filters = new LinearLayout(this);
        filters.setOrientation(LinearLayout.HORIZONTAL);
        filters.setPadding(0, 0, 0, Ui.dp(this, 12));
        addFilter(filters, "Pending", "Pending");
        addFilter(filters, "Confirmed", "Confirmed");
        addFilter(filters, "Failed", "Failed");
        addFilter(filters, "All", "");
        root.addView(filters);

        if (payments.length() == 0) {
            LinearLayout empty = Ui.card(this);
            empty.addView(Ui.text(this, "Queue Clear", 16, Ui.green(this)));
            empty.addView(Ui.body(this, "No payments currently match the " + (statusFilter.isEmpty() ? "selected" : statusFilter) + " filter."));
            root.addView(empty);
            return;
        }

        for (int i = 0; i < payments.length(); i++) {
            JSONObject payment = payments.optJSONObject(i);
            if (payment == null) continue;
            int id = payment.optInt("paymentId");

            LinearLayout card = Ui.card(this);
            LinearLayout topRow = new LinearLayout(this);
            topRow.setOrientation(LinearLayout.HORIZONTAL);
            topRow.setGravity(Gravity.CENTER_VERTICAL);

            LinearLayout leftCol = new LinearLayout(this);
            leftCol.setOrientation(LinearLayout.VERTICAL);

            TextView resName = Ui.text(this, payment.optString("residentName", "Resident"), 16, Ui.green(this));
            resName.setTypeface(Ui.serif());
            leftCol.addView(resName);

            String ref = payment.optString("categoryName", "Tariff") + "  ·  Ref: " + payment.optString("referenceNumber");
            TextView meta = Ui.text(this, ref, 12, Ui.muted(this));
            meta.setPadding(0, Ui.dp(this, 2), 0, 0);
            leftCol.addView(meta);

            topRow.addView(leftCol, new LinearLayout.LayoutParams(0, -2, 1f));

            LinearLayout rightCol = new LinearLayout(this);
            rightCol.setOrientation(LinearLayout.VERTICAL);
            rightCol.setGravity(Gravity.END);

            TextView amt = Ui.text(this, Format.money(payment.optDouble("amount", 0)), 16, Ui.green(this));
            amt.setTypeface(Ui.sansBold());
            rightCol.addView(amt);
            rightCol.addView(Ui.status(this, payment.optString("status")));
            topRow.addView(rightCol);

            card.addView(topRow);
            card.setClickable(true);
            card.setFocusable(true);
            card.setOnClickListener(v -> {
                Intent intent = new Intent(this, PaymentDetailActivity.class);
                intent.putExtra("paymentId", id);
                startActivity(intent);
            });
            root.addView(card);
        }
    }

    private void addFilter(LinearLayout row, String label, String value) {
        boolean selected = value.equalsIgnoreCase(statusFilter);
        TextView tab = new TextView(this);
        tab.setText(label);
        tab.setGravity(Gravity.CENTER);
        tab.setTextSize(12);
        tab.setTypeface(Ui.sansBold());
        tab.setTextColor(selected ? 0xFF051321 : Ui.muted(this));

        GradientDrawable pill = new GradientDrawable();
        pill.setCornerRadius(Ui.dp(this, 14));
        if (selected) {
            pill.setOrientation(GradientDrawable.Orientation.TL_BR);
            pill.setColors(new int[]{0xFFE8CA7C, 0xFFC4A35A});
        } else {
            pill.setColor(Ui.cardColor(this));
            pill.setStroke(Ui.dp(this, 1), Ui.line(this));
        }
        tab.setBackground(pill);

        int padH = Ui.dp(this, 10);
        int padV = Ui.dp(this, 8);
        tab.setPadding(padH, padV, padH, padV);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, -2, 1f);
        params.setMargins(Ui.dp(this, 2), 0, Ui.dp(this, 2), 0);
        tab.setLayoutParams(params);

        tab.setClickable(true);
        tab.setFocusable(true);
        tab.setOnClickListener(v -> {
            statusFilter = value;
            load();
        });
        row.addView(tab);
    }
}
