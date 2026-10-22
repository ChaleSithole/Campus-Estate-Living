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

public class StaffMessagesActivity extends CelActivity {
    private JSONArray contacts = new JSONArray();
    private int peerId;

    @Override protected void onCreate(Bundle state) {
        super.onCreate(state);
        if (!allowCategory(Roles.UI_ADMIN, Roles.UI_FINANCE)) return;
        bind("Messages");
        loadContacts();
    }

    private void loadContacts() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading administrator and financial officer contacts..."));
        io(() -> {
            JSONArray loaded = ApiClient.getArray("api/StaffMessages/contacts", session.getToken());
            runOnUiThread(() -> showContacts(loaded));
        });
    }

    private void showContacts(JSONArray loaded) {
        contacts = loaded;
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "STAFF CHANNEL"));
        root.addView(Ui.title(this, "Messages"));
        root.addView(Ui.body(this, "Private conversations with administrators and financial officers."));
        if (contacts.length() == 0) {
            LinearLayout empty = Ui.card(this);
            empty.addView(Ui.text(this, "No colleagues available", 16, Ui.ink(this)));
            empty.addView(Ui.body(this, "Active administrators and financial officers will appear here."));
            root.addView(empty);
            return;
        }
        for (int i = 0; i < contacts.length(); i++) {
            JSONObject person = contacts.optJSONObject(i);
            if (person == null) continue;
            int id = person.optInt("userId");
            String name = person.optString("fullName", "Staff member");
            String role = person.optString("role", "Staff").replace("FinancialOfficer", "Financial Officer");
            String latest = person.optString("latestMessage", "");
            int unread = person.optInt("unreadCount", 0);
            String detail = latest.isEmpty() ? role + "  ·  Start a conversation" : role + "  ·  " + latest;
            if (unread > 0) detail = unread + " unread  ·  " + detail;
            root.addView(Ui.tappable(this, name, detail, () -> openThread(id, name, role)));
        }
    }

    private void openThread(int id, String name, String role) {
        peerId = id;
        root.removeAllViews();
        Button back = Ui.button(this, "All messages", false);
        back.setOnClickListener(v -> showContacts(contacts));
        root.addView(back);
        root.addView(Ui.eyebrow(this, role.toUpperCase()));
        root.addView(Ui.title(this, name));
        root.addView(Ui.body(this, "Staff conversation"));
        LinearLayout thread = Ui.card(this);
        thread.setTag("staff_thread");
        root.addView(thread);
        EditText compose = Ui.field(this, "Write a message...", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        compose.setMinLines(2);
        compose.setMaxLines(5);
        root.addView(compose);
        Button send = Ui.button(this, "Send message", true);
        send.setOnClickListener(v -> {
            String body = compose.getText().toString().trim();
            if (body.isEmpty()) { toast("Write a message before sending."); return; }
            send.setEnabled(false);
            io(() -> {
                JSONObject request = new JSONObject();
                request.put("body", body);
                ApiClient.post("api/StaffMessages/" + peerId, request, session.getToken());
                JSONObject conversation = ApiClient.getObject("api/StaffMessages/" + peerId, session.getToken());
                runOnUiThread(() -> {
                    compose.setText("");
                    send.setEnabled(true);
                    renderThread(thread, conversation);
                });
            });
        });
        root.addView(send);
        io(() -> {
            JSONObject conversation = ApiClient.getObject("api/StaffMessages/" + id, session.getToken());
            runOnUiThread(() -> renderThread(thread, conversation));
        });
    }

    private void renderThread(LinearLayout thread, JSONObject conversation) {
        thread.removeAllViews();
        JSONArray messages = conversation.optJSONArray("messages");
        if (messages == null || messages.length() == 0) {
            thread.addView(Ui.body(this, "No messages yet. Start the conversation below."));
            return;
        }
        for (int i = 0; i < messages.length(); i++) {
            JSONObject message = messages.optJSONObject(i);
            if (message == null) continue;
            boolean mine = message.optInt("senderUserId") == session.getUserId();
            LinearLayout bubble = new LinearLayout(this);
            bubble.setOrientation(LinearLayout.VERTICAL);
            int pad = Ui.dp(this, 12);
            bubble.setPadding(pad, pad, pad, pad);
            android.graphics.drawable.GradientDrawable shape = new android.graphics.drawable.GradientDrawable();
            shape.setColor(mine ? (Ui.night(this) ? 0xFF173B5E : 0xFFEAF0F7) : Ui.cardColor(this));
            shape.setCornerRadius(Ui.dp(this, 16));
            shape.setStroke(Ui.dp(this, 1), Ui.line(this));
            bubble.setBackground(shape);
            TextView text = Ui.text(this, message.optString("body"), 14, Ui.ink(this));
            bubble.addView(text);
            TextView time = Ui.text(this, Format.when(message.optString("sentAt")), 10, Ui.muted(this));
            time.setPadding(0, Ui.dp(this, 5), 0, 0);
            bubble.addView(time);
            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(mine ? -2 : -1, -2);
            params.gravity = mine ? android.view.Gravity.END : android.view.Gravity.START;
            params.bottomMargin = Ui.dp(this, 8);
            params.width = -1;
            thread.addView(bubble, params);
        }
    }
}
