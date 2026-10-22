package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

public class NotificationsActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(session.isAdministratorUi() ? "Notifications" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading alerts..."));
        io(() -> {
            JSONArray items = ApiClient.getArray("api/Notifications", session.getToken());
            runOnUiThread(() -> show(items));
        });
    }

    private void show(JSONArray items) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Estate Inbox"));
        root.addView(Ui.title(this, "Notifications"));
        boolean anyUnread = false;
        for (int i = 0; i < items.length(); i++) {
            JSONObject probe = items.optJSONObject(i);
            if (probe != null && !probe.optBoolean("isRead")) anyUnread = true;
        }
        if (anyUnread) {
            android.widget.Button markAll = Ui.button(this, "Mark all as read", false);
            markAll.setOnClickListener(v -> io(() -> {
                ApiClient.post("api/Notifications/read-all", new JSONObject(), session.getToken());
                runOnUiThread(this::load);
            }));
            root.addView(markAll);
        }

        if (items.length() == 0) {
            LinearLayout empty = Ui.card(this);
            empty.addView(Ui.text(this, "All caught up", 16, Ui.green(this)));
            empty.addView(Ui.body(this, "You have no new alerts or notices from the estate administration."));
            root.addView(empty);
            return;
        }

        for (int i = 0; i < items.length(); i++) {
            JSONObject item = items.optJSONObject(i);
            if (item == null) continue;
            int id = item.optInt("notificationId");
            boolean unread = !item.optBoolean("isRead");
            int paymentId = item.isNull("paymentId") ? 0 : item.optInt("paymentId");

            LinearLayout card = Ui.card(this);
            if (unread) {
                // Subtle highlight border for unread cards
                GradientDrawable unreadBg = new GradientDrawable();
                unreadBg.setCornerRadius(Ui.dp(this, 16));
                unreadBg.setColor(Ui.night(this) ? 0xFF0E1A29 : 0xFFFDFEFE);
                unreadBg.setStroke(Ui.dp(this, 1.5f), Ui.GOLD);
                card.setBackground(unreadBg);
            }

            LinearLayout topRow = new LinearLayout(this);
            topRow.setOrientation(LinearLayout.HORIZONTAL);
            topRow.setGravity(Gravity.CENTER_VERTICAL);

            if (unread) {
                View dot = new View(this);
                GradientDrawable dotD = new GradientDrawable();
                dotD.setShape(GradientDrawable.OVAL);
                dotD.setColor(Ui.GOLD);
                dot.setBackground(dotD);
                int size = Ui.dp(this, 8);
                LinearLayout.LayoutParams dp = new LinearLayout.LayoutParams(size, size);
                dp.setMargins(0, 0, Ui.dp(this, 8), 0);
                dot.setLayoutParams(dp);
                topRow.addView(dot);
            }

            TextView titleView = Ui.text(this, item.optString("title"), 16, Ui.green(this));
            titleView.setTypeface(Ui.sansBold());
            topRow.addView(titleView, new LinearLayout.LayoutParams(0, -2, 1f));

            topRow.addView(Ui.status(this, unread ? "New" : "Read"));
            card.addView(topRow);

            // Subtitle: Type & timestamp
            TextView meta = Ui.text(this, item.optString("notificationType", "Notice") + "  ·  " + Format.when(item.optString("createdAt")), 12, Ui.muted(this));
            meta.setPadding(0, Ui.dp(this, 2), 0, Ui.dp(this, 8));
            card.addView(meta);

            // Body message
            TextView messageView = Ui.body(this, item.optString("message"));
            messageView.setPadding(0, 0, 0, paymentId > 0 ? Ui.dp(this, 6) : 0);
            card.addView(messageView);

            if (paymentId > 0) {
                TextView link = Ui.text(this, "View associated payment receipt", 13, Ui.GOLD);
                link.setTypeface(Ui.sansBold());
                card.addView(link);
            }

            card.setClickable(true);
            card.setFocusable(true);
            card.setOnClickListener(v -> mark(id, unread, paymentId));
            root.addView(card);
        }
    }

    private void mark(int id, boolean unread, int paymentId) {
        io(() -> {
            if (unread) {
                ApiClient.post("api/Notifications/" + id + "/read", new org.json.JSONObject(), session.getToken());
            }
            runOnUiThread(() -> {
                if (paymentId > 0) {
                    Intent intent = new Intent(this, PaymentDetailActivity.class);
                    intent.putExtra("paymentId", paymentId);
                    startActivity(intent);
                } else {
                    load();
                }
            });
        });
    }
}
