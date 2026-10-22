package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.text.InputType;
import android.widget.Button;
import android.widget.EditText;

import org.json.JSONArray;
import org.json.JSONObject;

public class ReviewsActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading reviews..."));
        String path = session.isResident() ? "api/Reviews/mine" : "api/Reviews";
        io(() -> {
            JSONArray reviews = ApiClient.getArray(path, session.getToken());
            runOnUiThread(() -> show(reviews));
        });
    }

    private void show(JSONArray reviews) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Reviews"));
        root.addView(Ui.title(this, session.isResident() ? "Your reviews" : "Resident reviews"));
        if (reviews.length() == 0) root.addView(Ui.body(this, "No reviews yet."));
        for (int i = 0; i < reviews.length(); i++) {
            JSONObject review = reviews.optJSONObject(i);
            if (review == null) continue;
            android.widget.LinearLayout row = Ui.card(this);
            row.addView(Ui.text(this, review.optInt("rating") + " / 5 · " + review.optString("authorName"), 16, Ui.ink(this)));
            row.addView(Ui.body(this, Format.when(review.optString("createdAt")) + "\n" + review.optString("comment")));
            root.addView(row);
        }
        if (!session.isResident()) return;
        root.addView(Ui.text(this, "Write a review", 18, Ui.green(this)));
        EditText rating = Ui.field(this, "Rating from 1 to 5", InputType.TYPE_CLASS_NUMBER);
        EditText comment = Ui.field(this, "Comment", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        comment.setSingleLine(false);
        comment.setMinLines(3);
        root.addView(rating);
        root.addView(comment);
        Button send = Ui.button(this, "Submit review", true);
        send.setOnClickListener(v -> {
            String rawRating = rating.getText().toString().trim();
            String text = comment.getText().toString().trim();
            int stars;
            try {
                stars = Integer.parseInt(rawRating);
            } catch (NumberFormatException error) {
                toast("Rating must be between 1 and 5.");
                return;
            }
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("rating", stars);
                body.put("comment", text);
                ApiClient.post("api/Reviews", body, session.getToken());
                runOnUiThread(() -> {
                    toast("Review recorded.");
                    load();
                });
            });
        });
        root.addView(send);
    }
}
