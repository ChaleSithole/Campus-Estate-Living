package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.os.Bundle;
import android.view.Gravity;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONObject;

public class PaymentDetailActivity extends CelActivity {
    private int paymentId;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        paymentId = getIntent().getIntExtra("paymentId", 0);
        bind(null);
        root.addView(Ui.body(this, "Loading payment details..."));
        load();
    }

    private void load() {
        io(() -> {
            JSONObject payment = ApiClient.getObject("api/Payments/" + paymentId, session.getToken());
            runOnUiThread(() -> show(payment));
        });
    }

    private void show(JSONObject payment) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Receipt · " + payment.optString("referenceNumber")));
        root.addView(Ui.title(this, payment.optString("categoryName")));

        // High-end Receipt Card
        LinearLayout receipt = Ui.card(this);

        LinearLayout topBar = new LinearLayout(this);
        topBar.setOrientation(LinearLayout.HORIZONTAL);
        topBar.setGravity(Gravity.CENTER_VERTICAL);
        TextView recLabel = Ui.eyebrow(this, "Official Transaction Receipt");
        topBar.addView(recLabel, new LinearLayout.LayoutParams(0, -2, 1f));
        topBar.addView(Ui.status(this, payment.optString("status")));
        receipt.addView(topBar);

        TextView amountView = Ui.text(this, Format.money(payment.optDouble("amount")), 30, Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY);
        amountView.setTypeface(Ui.serif());
        amountView.setPadding(0, Ui.dp(this, 6), 0, Ui.dp(this, 12));
        receipt.addView(amountView);

        Ui.pair(receipt, "Reference", payment.optString("referenceNumber"));
        Ui.pair(receipt, "Method", payment.optString("paymentMethod"));
        Ui.pair(receipt, "Resident", payment.optString("residentName"));
        Ui.pair(receipt, "Account", payment.optString("accountNumber"));
        Ui.pair(receipt, "Payment Date", Format.when(payment.optString("paymentDate")));
        Ui.pair(receipt, "Description", Format.text(payment.optString("notes")));

        if (!payment.isNull("cardLast4") && !payment.optString("cardLast4").isEmpty()) {
            Ui.pair(receipt, "Card Number", "•••• " + payment.optString("cardLast4"));
        }
        if (!payment.isNull("processedBy") && !payment.optString("processedBy").isEmpty()) {
            Ui.pair(receipt, "Processed By", Format.text(payment.optString("processedBy")));
            Ui.pair(receipt, "Processed At", Format.when(payment.optString("processedAt")));
        }

        root.addView(receipt);

        boolean pending = "Pending".equalsIgnoreCase(payment.optString("status"));
        if (pending && session.isResident()) {
            Button cancel = Ui.button(this, "Cancel pending payment", false);
            setButtonIcon(cancel, R.drawable.ic_checkout_back);
            cancel.setOnClickListener(v -> Ui.confirmation(this, "Cancel pending payment?", "This payment will be marked as cancelled on your Campus Estate Living account.")
                    .setTitle("Cancel this payment?")
                    .setMessage("The pending payment will be marked as cancelled.")
                    .setIcon(R.drawable.ic_checkout_back)
                    .setNegativeButton("Keep payment", (dialog, which) -> dialog.dismiss())
                    .setPositiveButton("Cancel payment", (dialog, which) -> io(() -> {
                        ApiClient.post("api/Payments/" + paymentId + "/cancel", new JSONObject(), session.getToken());
                        runOnUiThread(() -> {
                            toast("Payment cancelled.");
                            load();
                        });
                    }))
                    .show());
            root.addView(cancel);
        }

        if (pending && session.isStaffSide()) {
            Button confirm = Ui.button(this, "Confirm payment", true);
            setButtonIcon(confirm, R.drawable.ic_checkout_check);
            confirm.setOnClickListener(v -> Ui.confirmation(this, "Confirm payment?", "Confirming records this payment as verified in Campus Estate Living.")
                    .setTitle("Confirm this payment?")
                    .setMessage("This payment will be marked as confirmed and cleared from the pending queue.")
                    .setIcon(R.drawable.ic_checkout_check)
                    .setNegativeButton("Go back", (dialog, which) -> dialog.dismiss())
                    .setPositiveButton("Confirm", (dialog, which) -> act("confirm", "Payment confirmed successfully."))
                    .show());
            Button fail = Ui.button(this, "Mark as failed", false);
            setButtonIcon(fail, R.drawable.ic_checkout_back);
            fail.setOnClickListener(v -> Ui.confirmation(this, "Mark payment as failed?", "This payment will be marked as unsuccessful in Campus Estate Living.")
                    .setTitle("Mark this payment as failed?")
                    .setMessage("The resident will be notified that this payment could not be verified.")
                    .setNegativeButton("Go back", (dialog, which) -> dialog.dismiss())
                    .setPositiveButton("Mark as failed", (dialog, which) -> act("fail", "Payment marked as failed."))
                    .show());
            root.addView(confirm);
            root.addView(fail);
        }

        if (session.isStaffSide()) {
            int accountId = payment.optInt("accountId");
            Button record = Ui.button(this, "Record payment for this account", false);
            setButtonIcon(record, R.drawable.ic_nav_card);
            record.setOnClickListener(v -> {
                Intent intent = new Intent(this, RecordPaymentActivity.class);
                intent.putExtra("accountId", accountId);
                intent.putExtra("residentName", payment.optString("residentName"));
                startActivity(intent);
            });
            root.addView(record);
        }

        Button back = Ui.button(this, "Back", false);
        setButtonIcon(back, R.drawable.ic_checkout_back);
        back.setOnClickListener(v -> finish());
        root.addView(back);
    }

    private void act(String action, String success) {
        io(() -> {
            ApiClient.post("api/Payments/" + paymentId + "/" + action, new JSONObject(), session.getToken());
            runOnUiThread(() -> {
                toast(success);
                load();
            });
        });
    }

    private void setButtonIcon(Button button, int resource) {
        android.graphics.drawable.Drawable icon = getDrawable(resource);
        if (icon != null) icon.setTint(button.getCurrentTextColor());
        button.setCompoundDrawablesRelativeWithIntrinsicBounds(icon, null, null, null);
        button.setCompoundDrawablePadding(Ui.dp(this, 8));
    }
}
