package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.text.InputType;
import android.view.Gravity;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.Spinner;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

public class NoticesActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(session.isAdministratorUi() ? "Notices" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading campus notices..."));
        io(() -> {
            JSONArray notices = ApiClient.getArray("api/Notices", session.getToken());
            runOnUiThread(() -> show(notices));
        });
    }

    private void show(JSONArray notices) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Estate Gazette"));
        root.addView(Ui.title(this, "Notices & Circulars"));

        if (notices.length() == 0) {
            LinearLayout empty = Ui.card(this);
            empty.addView(Ui.text(this, "No official notices", 16, Ui.green(this)));
            empty.addView(Ui.body(this, "The estate office has not published any notices at this time."));
            root.addView(empty);
        }

        boolean admin = session.hasRole(Roles.ADMINISTRATOR);
        for (int i = 0; i < notices.length(); i++) {
            JSONObject notice = notices.optJSONObject(i);
            if (notice == null) continue;
            int id = notice.optInt("announcementId");
            boolean pinned = notice.optBoolean("isPinned");

            LinearLayout card = Ui.card(this);
            if (pinned) {
                GradientDrawable pBg = new GradientDrawable();
                pBg.setCornerRadius(Ui.dp(this, 16));
                pBg.setColor(Ui.night(this) ? 0xFF0E1A29 : 0xFFFAF8F2);
                pBg.setStroke(Ui.dp(this, 1.5f), Ui.GOLD);
                card.setBackground(pBg);
            }

            LinearLayout topBar = new LinearLayout(this);
            topBar.setOrientation(LinearLayout.HORIZONTAL);
            topBar.setGravity(Gravity.CENTER_VERTICAL);

            TextView audienceV = Ui.eyebrow(this, "Audience: " + notice.optString("audience", "All"));
            topBar.addView(audienceV, new LinearLayout.LayoutParams(0, -2, 1f));

            if (pinned) {
                topBar.addView(Ui.status(this, "★ Pinned"));
            }
            card.addView(topBar);

            TextView nTitle = Ui.text(this, notice.optString("title"), 18, Ui.green(this));
            nTitle.setTypeface(Ui.serif());
            nTitle.setPadding(0, Ui.dp(this, 4), 0, Ui.dp(this, 2));
            card.addView(nTitle);

            TextView timeV = Ui.text(this, "Published " + Format.when(notice.optString("publishedAt")), 12, Ui.muted(this));
            timeV.setPadding(0, 0, 0, Ui.dp(this, 8));
            card.addView(timeV);

            card.addView(Ui.body(this, notice.optString("body")));

            if (admin) {
                Button remove = Ui.button(this, "Delete notice", false);
                remove.setOnClickListener(v -> io(() -> {
                    ApiClient.delete("api/Notices/" + id, session.getToken());
                    runOnUiThread(() -> {
                        toast("Notice deleted.");
                        load();
                    });
                }));
                card.addView(remove);
            }
            root.addView(card);
        }

        if (!admin) return;

        LinearLayout publishCard = Ui.card(this);
        publishCard.addView(Ui.text(this, "Publish Official Notice", 18, Ui.green(this)));

        EditText title = Ui.field(this, "Notice headline / subject", InputType.TYPE_CLASS_TEXT);
        EditText body = Ui.field(this, "Official circular message text...", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        body.setSingleLine(false);
        body.setMinLines(3);

        publishCard.addView(Ui.text(this, "Target Audience", 12, Ui.muted(this)));
        Spinner audience = new Spinner(this);
        audience.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item,
                new String[]{"All", "Residents", "Officers"}));
        audience.setPadding(Ui.dp(this, 8), Ui.dp(this, 8), Ui.dp(this, 8), Ui.dp(this, 8));

        CheckBox pinned = new CheckBox(this);
        pinned.setText("Pin to top of feed");
        pinned.setTextColor(Ui.ink(this));

        publishCard.addView(title);
        publishCard.addView(body);
        publishCard.addView(audience);
        publishCard.addView(pinned);

        Button publish = Ui.button(this, "Publish announcement", true);
        publish.setOnClickListener(v -> {
            String heading = title.getText().toString().trim();
            String text = body.getText().toString().trim();
            String who = String.valueOf(audience.getSelectedItem());
            boolean pin = pinned.isChecked();
            if (heading.isEmpty() || text.isEmpty()) {
                toast("Please provide both headline and message body.");
                return;
            }
            io(() -> {
                JSONObject payload = new JSONObject();
                payload.put("title", heading);
                payload.put("body", text);
                payload.put("audience", who);
                payload.put("isPinned", pin);
                ApiClient.post("api/Notices", payload, session.getToken());
                runOnUiThread(() -> {
                    toast("Notice published successfully.");
                    load();
                });
            });
        });
        publishCard.addView(publish);
        root.addView(publishCard);
    }
}
