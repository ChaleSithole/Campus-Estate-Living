package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.text.InputType;
import android.view.Gravity;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.HorizontalScrollView;
import android.widget.Spinner;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.net.URLEncoder;

public class MaintenanceActivity extends CelActivity {
    private String statusFilter = "";
    private static final String[] CATEGORIES = {"Plumbing", "Electrical", "Furniture", "Security", "General", "HVAC / Heating"};
    private static final String[] PRIORITIES = {"Low", "Normal", "High", "Urgent"};
    private static final String[] STATUSES = {"Open", "In progress", "Resolved", "Closed"};

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        bind(session.isAdministratorUi() ? "Maintenance" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading maintenance tickets..."));
        io(() -> {
            String path = statusFilter.isEmpty()
                    ? "api/Maintenance"
                    : "api/Maintenance?status=" + URLEncoder.encode(statusFilter, "UTF-8");
            JSONArray items = ApiClient.getArray(path, session.getToken());
            runOnUiThread(() -> show(items));
        });
    }

    private void show(JSONArray items) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Facilities & Estate"));
        root.addView(Ui.title(this, "Maintenance"));

        // Scrollable filter chips keep labels legible on narrow screens.
        HorizontalScrollView filterScroll = new HorizontalScrollView(this);
        filterScroll.setHorizontalScrollBarEnabled(false);
        LinearLayout filters = new LinearLayout(this);
        filters.setOrientation(LinearLayout.HORIZONTAL);
        filters.setPadding(0, 0, 0, Ui.dp(this, 12));
        addFilter(filters, "All", "");
        addFilter(filters, "Open", "Open");
        addFilter(filters, "In Progress", "In progress");
        addFilter(filters, "Resolved", "Resolved");
        filterScroll.addView(filters);
        root.addView(filterScroll);

        if (session.isResident()) {
            Button create = Ui.button(this, "Log maintenance issue", true);
            create.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_quick_faults, 0, 0, 0);
            create.setCompoundDrawablePadding(Ui.dp(this, 10));
            create.setCompoundDrawableTintList(android.content.res.ColorStateList.valueOf(Ui.onAccent(this)));
            create.setOnClickListener(v -> showCreate());
            root.addView(create);
        }

        if (items.length() == 0) {
            LinearLayout empty = Ui.card(this);
            empty.addView(Ui.text(this, "No maintenance requests", 16, Ui.green(this)));
            empty.addView(Ui.body(this, "There are currently no tickets matching the selected filter."));
            root.addView(empty);
            return;
        }

        for (int i = 0; i < items.length(); i++) {
            JSONObject item = items.optJSONObject(i);
            if (item == null) continue;

            LinearLayout card = Ui.card(this);
            LinearLayout topRow = new LinearLayout(this);
            topRow.setOrientation(LinearLayout.HORIZONTAL);
            topRow.setGravity(Gravity.CENTER_VERTICAL);

            LinearLayout leftCol = new LinearLayout(this);
            leftCol.setOrientation(LinearLayout.VERTICAL);

            TextView title = Ui.text(this, item.optString("title"), 16, Ui.green(this));
            title.setTypeface(Ui.sansBold());
            leftCol.addView(title);

            String catAndPri = item.optString("category", "General") + "  ·  Priority: " + item.optString("priority", "Normal");
            TextView meta = Ui.text(this, catAndPri, 12, Ui.muted(this));
            meta.setPadding(0, Ui.dp(this, 2), 0, 0);
            leftCol.addView(meta);

            if (session.isStaffSide() && !item.isNull("residentName")) {
                TextView res = Ui.text(this, "Resident: " + item.optString("residentName"), 12, Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY);
                res.setPadding(0, Ui.dp(this, 2), 0, 0);
                leftCol.addView(res);
            }

            topRow.addView(leftCol, new LinearLayout.LayoutParams(0, -2, 1f));
            topRow.addView(Ui.status(this, item.optString("status")));
            card.addView(topRow);

            card.setClickable(true);
            card.setFocusable(true);
            card.setOnClickListener(v -> showOne(item));
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

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-2, -2);
        params.setMargins(0, 0, Ui.dp(this, 8), 0);
        tab.setLayoutParams(params);

        tab.setClickable(true);
        tab.setFocusable(true);
        tab.setOnClickListener(v -> {
            statusFilter = value;
            load();
        });
        row.addView(tab);
    }

    private void showCreate() {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Estate Facilities"));
        root.addView(Ui.title(this, "Log a ticket"));

        LinearLayout formCard = Ui.card(this);
        formCard.addView(Ui.text(this, "Issue Category", 13, Ui.muted(this)));
        Spinner category = spinnerInView(formCard, CATEGORIES);

        formCard.addView(Ui.text(this, "Urgency / Priority", 13, Ui.muted(this)));
        Spinner priority = spinnerInView(formCard, PRIORITIES);

        EditText title = Ui.field(this, "Issue summary (e.g. Leaking shower)", InputType.TYPE_CLASS_TEXT);
        EditText description = Ui.field(this, "Detailed explanation of the issue...", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        description.setSingleLine(false);
        description.setMinLines(4);

        formCard.addView(title);
        formCard.addView(description);

        Button send = Ui.button(this, "Submit maintenance ticket", true);
        send.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_checkout_check, 0, 0, 0);
        send.setCompoundDrawablePadding(Ui.dp(this, 10));
        send.setCompoundDrawableTintList(android.content.res.ColorStateList.valueOf(Ui.onAccent(this)));
        send.setOnClickListener(v -> {
            String chosenCategory = String.valueOf(category.getSelectedItem());
            String chosenPriority = String.valueOf(priority.getSelectedItem());
            String heading = title.getText().toString().trim();
            String text = description.getText().toString().trim();
            if (heading.isEmpty() || text.isEmpty()) {
                toast("Please provide both a title and description.");
                return;
            }
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("category", chosenCategory);
                body.put("priority", chosenPriority);
                body.put("title", heading);
                body.put("description", text);
                ApiClient.post("api/Maintenance", body, session.getToken());
                runOnUiThread(() -> {
                    toast("Maintenance request submitted to estate team.");
                    load();
                });
            });
        });
        formCard.addView(send);
        root.addView(formCard);

        Button back = Ui.button(this, "Cancel", false);
        back.setOnClickListener(v -> load());
        root.addView(back);
    }

    private void showOne(JSONObject item) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Ticket #" + item.optInt("requestId")));
        root.addView(Ui.title(this, item.optString("title")));

        LinearLayout card = Ui.card(this);
        LinearLayout topBar = new LinearLayout(this);
        topBar.setOrientation(LinearLayout.HORIZONTAL);
        topBar.setGravity(Gravity.CENTER_VERTICAL);
        topBar.addView(Ui.eyebrow(this, item.optString("category", "General")), new LinearLayout.LayoutParams(0, -2, 1f));
        topBar.addView(Ui.status(this, item.optString("status")));
        card.addView(topBar);

        Ui.pair(card, "Priority", item.optString("priority"));
        Ui.pair(card, "Resident", item.optString("residentName"));
        String building = item.optString("buildingName", "");
        Ui.pair(card, "Room", building.isEmpty() || "null".equals(building)
                ? "Not assigned" : building + " · Room " + item.optString("roomNumber"));
        Ui.pair(card, "Logged Date", Format.when(item.optString("createdAt")));
        Ui.pair(card, "Last Updated", Format.when(item.optString("updatedAt")));

        TextView descTitle = Ui.text(this, "Description", 14, Ui.green(this));
        descTitle.setPadding(0, Ui.dp(this, 10), 0, Ui.dp(this, 4));
        card.addView(descTitle);
        card.addView(Ui.body(this, item.optString("description")));

        String staffNotes = item.optString("staffNotes", "");
        if (!staffNotes.isEmpty() && !"null".equalsIgnoreCase(staffNotes)) {
            TextView sNoteTitle = Ui.text(this, "Estate Office Notes", 14, Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY);
            sNoteTitle.setPadding(0, Ui.dp(this, 10), 0, Ui.dp(this, 4));
            card.addView(sNoteTitle);
            card.addView(Ui.body(this, staffNotes));
        }

        root.addView(card);
        int id = item.optInt("requestId");

        if (session.isStaffSide()) {
            LinearLayout staffAction = Ui.card(this);
            staffAction.addView(Ui.text(this, "Manage Ticket Status", 16, Ui.green(this)));

            staffAction.addView(Ui.text(this, "Status", 13, Ui.muted(this)));
            Spinner status = spinnerInView(staffAction, STATUSES);
            select(status, STATUSES, item.optString("status"));

            staffAction.addView(Ui.text(this, "Priority", 13, Ui.muted(this)));
            Spinner priority = spinnerInView(staffAction, PRIORITIES);
            select(priority, PRIORITIES, item.optString("priority"));

            EditText notes = Ui.field(this, "Estate / technician notes", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
            notes.setSingleLine(false);
            if (!"null".equals(staffNotes)) notes.setText(staffNotes);
            staffAction.addView(notes);

            Button save = Ui.button(this, "Update ticket status", true);
            save.setOnClickListener(v -> {
                String chosenStatus = String.valueOf(status.getSelectedItem());
                String chosenPriority = String.valueOf(priority.getSelectedItem());
                String notesVal = notes.getText().toString().trim();
                io(() -> {
                    JSONObject body = new JSONObject();
                    body.put("status", chosenStatus);
                    body.put("priority", chosenPriority);
                    body.put("staffNotes", notesVal);
                    ApiClient.put("api/Maintenance/" + id, body, session.getToken());
                    runOnUiThread(() -> {
                        toast("Ticket updated successfully.");
                        load();
                    });
                });
            });
            staffAction.addView(save);
            root.addView(staffAction);
        }

        Button back = Ui.button(this, "Back to maintenance", false);
        back.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_checkout_back, 0, 0, 0);
        back.setCompoundDrawablePadding(Ui.dp(this, 10));
        back.setCompoundDrawableTintList(android.content.res.ColorStateList.valueOf(Ui.green(this)));
        back.setOnClickListener(v -> load());
        root.addView(back);
    }

    private Spinner spinnerInView(LinearLayout parent, String[] values) {
        Spinner spinner = new Spinner(this);
        spinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, values));
        spinner.setPadding(Ui.dp(this, 8), Ui.dp(this, 8), Ui.dp(this, 8), Ui.dp(this, 8));
        parent.addView(spinner);
        return spinner;
    }

    private void select(Spinner spinner, String[] values, String current) {
        for (int i = 0; i < values.length; i++) {
            if (values[i].equalsIgnoreCase(current)) {
                spinner.setSelection(i);
                return;
            }
        }
    }
}
