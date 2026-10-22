package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.HorizontalScrollView;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.Calendar;
import java.util.Locale;
import java.text.SimpleDateFormat;

/** Resident transaction history and monthly statement. */
public class HistoryActivity extends CelActivity {
    private JSONArray payments;
    private LinearLayout list;
    private TextView resultCount;
    private String activeFilter = "All";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!allowCategory(Roles.UI_RESIDENT)) return;
        bind("History");
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading your history..."));
        Calendar now = Calendar.getInstance();
        int year = now.get(Calendar.YEAR);
        int month = now.get(Calendar.MONTH) + 1;
        io(() -> {
            JSONArray rows = ApiClient.getArray("api/Payments", session.getToken());
            JSONObject statement = null;
            String statementError = null;
            try {
                statement = ApiClient.getObject("api/Payments/statement?year=" + year + "&month=" + month, session.getToken());
            } catch (Exception error) {
                statementError = error.getMessage();
            }
            JSONObject ready = statement;
            String error = statementError;
            runOnUiThread(() -> show(rows, ready, error));
        });
    }

    private void show(JSONArray rows, JSONObject statement, String statementError) {
        payments = rows == null ? new JSONArray() : rows;
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "YOUR ACCOUNT"));
        root.addView(Ui.title(this, "Payment history"));

        String account = statement == null ? "" : statement.optString("accountNumber", "");
        String room = statement == null ? "" : statement.optString("roomLabel", "");
        String subtitle = account.isEmpty() ? "Payments and receipts on your estate account." : "Account " + account;
        if (!room.isEmpty() && !"null".equalsIgnoreCase(room)) subtitle += "  ·  " + room;
        root.addView(Ui.body(this, subtitle));

        double confirmed = statement == null ? sumForStatus("Confirmed") : statement.optDouble("confirmedTotal", 0);
        double pending = statement == null ? sumForStatus("Pending") : statement.optDouble("pendingTotal", 0);
        root.addView(Ui.eyebrow(this, "THIS MONTH"));
        root.addView(summaryStrip(confirmed, pending));

        Button pay = Ui.button(this, "Make a payment", true);
        pay.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_nav_transact, 0, 0, 0);
        pay.setCompoundDrawablePadding(Ui.dp(this, 10));
        pay.setCompoundDrawableTintList(android.content.res.ColorStateList.valueOf(Ui.onAccent(this)));
        pay.setOnClickListener(v -> open(PaymentsActivity.class));
        root.addView(pay);

        if (statementError != null && !statementError.trim().isEmpty()) {
            TextView notice = Ui.text(this, "Monthly statement unavailable. Showing your transaction history.", 12, Ui.muted(this));
            notice.setPadding(0, 0, 0, Ui.dp(this, 8));
            root.addView(notice);
        }

        LinearLayout heading = new LinearLayout(this);
        heading.setOrientation(LinearLayout.HORIZONTAL);
        heading.setGravity(Gravity.CENTER_VERTICAL);
        TextView title = Ui.text(this, "Transactions", 18, Ui.ink(this));
        title.setTypeface(Ui.sansBold());
        heading.addView(title, new LinearLayout.LayoutParams(0, -2, 1f));
        resultCount = Ui.text(this, "", 12, Ui.muted(this));
        heading.addView(resultCount);
        root.addView(heading);

        root.addView(filterBar());
        list = new LinearLayout(this);
        list.setOrientation(LinearLayout.VERTICAL);
        root.addView(list);
        renderPayments();
    }

    private LinearLayout summaryStrip(double confirmed, double pending) {
        LinearLayout strip = new LinearLayout(this);
        strip.setOrientation(LinearLayout.HORIZONTAL);
        strip.setPadding(0, 0, 0, Ui.dp(this, 8));
        strip.addView(summaryTile("Confirmed", confirmed, R.drawable.ic_checkout_check),
                new LinearLayout.LayoutParams(0, -2, 1f));
        View spacer = new View(this);
        strip.addView(spacer, new LinearLayout.LayoutParams(Ui.dp(this, 10), 1));
        strip.addView(summaryTile("Awaiting review", pending, R.drawable.ic_checkout_money),
                new LinearLayout.LayoutParams(0, -2, 1f));
        return strip;
    }

    private LinearLayout summaryTile(String label, double amount, int icon) {
        LinearLayout tile = Ui.card(this);
        tile.setPadding(Ui.dp(this, 14), Ui.dp(this, 14), Ui.dp(this, 14), Ui.dp(this, 14));
        LinearLayout top = new LinearLayout(this);
        top.setOrientation(LinearLayout.HORIZONTAL);
        top.setGravity(Gravity.CENTER_VERTICAL);
        ImageView image = new ImageView(this);
        image.setImageResource(icon);
        image.setColorFilter(Ui.green(this));
        top.addView(image, new LinearLayout.LayoutParams(Ui.dp(this, 18), Ui.dp(this, 18)));
        TextView caption = Ui.text(this, label, 11, Ui.muted(this));
        caption.setTypeface(Ui.sansBold());
        caption.setPadding(Ui.dp(this, 7), 0, 0, 0);
        top.addView(caption);
        tile.addView(top);
        TextView value = Ui.text(this, Format.money(amount), 17, Ui.ink(this));
        value.setTypeface(Ui.sansBold());
        value.setPadding(0, Ui.dp(this, 8), 0, 0);
        tile.addView(value);
        return tile;
    }

    private HorizontalScrollView filterBar() {
        HorizontalScrollView scroll = new HorizontalScrollView(this);
        scroll.setHorizontalScrollBarEnabled(false);
        LinearLayout filters = new LinearLayout(this);
        filters.setOrientation(LinearLayout.HORIZONTAL);
        filters.setPadding(0, Ui.dp(this, 8), 0, Ui.dp(this, 10));
        String[] values = {"All", "Pending", "Confirmed", "Failed", "Cancelled"};
        for (String filter : values) {
            Button chip = new Button(this);
            chip.setAllCaps(false);
            chip.setTextSize(12);
            chip.setMinHeight(Ui.dp(this, 38));
            chip.setMinimumHeight(Ui.dp(this, 38));
            chip.setPadding(Ui.dp(this, 14), 0, Ui.dp(this, 14), 0);
            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-2, Ui.dp(this, 38));
            params.rightMargin = Ui.dp(this, 8);
            filters.addView(chip, params);
            chip.setOnClickListener(v -> {
                activeFilter = filter;
                updateFilterStyles(filters, values);
                renderPayments();
            });
        }
        scroll.addView(filters);
        updateFilterStyles(filters, values);
        return scroll;
    }

    private void updateFilterStyles(LinearLayout filters, String[] values) {
        for (int i = 0; i < values.length; i++) {
            Button chip = (Button) filters.getChildAt(i);
            boolean selected = values[i].equals(activeFilter);
            GradientDrawable shape = new GradientDrawable();
            shape.setCornerRadius(Ui.dp(this, 20));
            shape.setColor(selected ? Ui.green(this) : Ui.cardColor(this));
            if (!selected) shape.setStroke(Ui.dp(this, 1), Ui.line(this));
            chip.setBackground(shape);
            chip.setTextColor(selected ? Ui.onAccent(this) : Ui.ink(this));
            int count = values[i].equals("All") ? payments.length() : countForStatus(values[i]);
            chip.setText(values[i] + "  " + count);
        }
    }

    private void renderPayments() {
        if (list == null || payments == null) return;
        list.removeAllViews();
        int shown = 0;
        for (int i = 0; i < payments.length(); i++) {
            JSONObject payment = payments.optJSONObject(i);
            if (payment == null) continue;
            String status = payment.optString("status", "Pending");
            if (!"All".equals(activeFilter) && !activeFilter.equalsIgnoreCase(status)) continue;
            int id = payment.optInt("paymentId");
            list.addView(transactionRow(payment, () -> {
                Intent intent = new Intent(this, PaymentDetailActivity.class);
                intent.putExtra("paymentId", id);
                startActivity(intent);
            }));
            shown++;
        }
        resultCount.setText(shown + (shown == 1 ? " payment" : " payments"));
        if (shown == 0) {
            TextView empty = Ui.body(this, "No " + ("All".equals(activeFilter) ? "transactions" : activeFilter.toLowerCase() + " payments") + " yet.");
            empty.setPadding(0, Ui.dp(this, 18), 0, Ui.dp(this, 12));
            list.addView(empty);
        }
    }

    private View transactionRow(JSONObject payment, Runnable action) {
        LinearLayout row = Ui.card(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER_VERTICAL);
        row.setPadding(Ui.dp(this, 14), Ui.dp(this, 12), Ui.dp(this, 12), Ui.dp(this, 12));

        FrameLayout iconBox = new FrameLayout(this);
        GradientDrawable iconBackground = new GradientDrawable();
        iconBackground.setCornerRadius(Ui.dp(this, 12));
        iconBackground.setColor(Ui.night(this) ? 0x332DAA91 : 0xFFE5F3F0);
        iconBox.setBackground(iconBackground);
        ImageView icon = new ImageView(this);
        icon.setImageResource("Card".equalsIgnoreCase(payment.optString("paymentMethod")) ? R.drawable.ic_nav_card : R.drawable.ic_checkout_bank);
        icon.setColorFilter(Ui.green(this));
        int iconSize = Ui.dp(this, 22);
        iconBox.addView(icon, new FrameLayout.LayoutParams(iconSize, iconSize, Gravity.CENTER));
        LinearLayout.LayoutParams iconParams = new LinearLayout.LayoutParams(Ui.dp(this, 42), Ui.dp(this, 42));
        iconParams.rightMargin = Ui.dp(this, 12);
        row.addView(iconBox, iconParams);

        LinearLayout details = new LinearLayout(this);
        details.setOrientation(LinearLayout.VERTICAL);
        TextView category = Ui.text(this, payment.optString("categoryName", "Payment"), 14, Ui.ink(this));
        category.setTypeface(Ui.sansBold());
        details.addView(category);
        String date = payment.optString("paymentDate", "");
        date = shortDate(date);
        String reference = payment.optString("referenceNumber", "");
        String method = payment.optString("paymentMethod", "EFT");
        if ("Card".equalsIgnoreCase(method) && !payment.optString("cardLast4", "").isEmpty()) method += " ···· " + payment.optString("cardLast4");
        TextView meta = Ui.text(this, date + "  ·  " + method + (reference.isEmpty() ? "" : "\nRef " + reference), 11, Ui.muted(this));
        meta.setPadding(0, Ui.dp(this, 4), 0, 0);
        details.addView(meta);
        row.addView(details, new LinearLayout.LayoutParams(0, -2, 1f));

        LinearLayout amountBlock = new LinearLayout(this);
        amountBlock.setOrientation(LinearLayout.VERTICAL);
        amountBlock.setGravity(Gravity.END);
        TextView amount = Ui.text(this, Format.money(payment.optDouble("amount", 0)), 13, Ui.ink(this));
        amount.setTypeface(Ui.sansBold());
        amountBlock.addView(amount);
        amountBlock.addView(Ui.status(this, payment.optString("status", "Pending")));
        row.addView(amountBlock);
        row.setClickable(true);
        row.setFocusable(true);
        row.setOnClickListener(v -> action.run());
        return row;
    }

    private int countForStatus(String status) {
        int count = 0;
        for (int i = 0; i < payments.length(); i++) {
            JSONObject payment = payments.optJSONObject(i);
            if (payment != null && status.equalsIgnoreCase(payment.optString("status"))) count++;
        }
        return count;
    }

    private double sumForStatus(String status) {
        double total = 0;
        for (int i = 0; i < payments.length(); i++) {
            JSONObject payment = payments.optJSONObject(i);
            if (payment != null && status.equalsIgnoreCase(payment.optString("status"))) total += payment.optDouble("amount", 0);
        }
        return total;
    }

    private String shortDate(String value) {
        if (value == null || value.length() < 10) return "Date unavailable";
        try {
            java.util.Date parsed = new SimpleDateFormat("yyyy-MM-dd", Locale.US).parse(value.substring(0, 10));
            return new SimpleDateFormat("dd MMM yyyy", Locale.getDefault()).format(parsed);
        } catch (Exception ignored) {
            return value.substring(0, 10);
        }
    }
}
