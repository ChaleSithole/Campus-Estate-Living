package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.app.AlertDialog;
import android.content.Intent;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.text.InputType;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.RadioButton;
import android.widget.RadioGroup;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.AdapterView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class PaymentsActivity extends CelActivity {
    private String statusFilter = "";
    private final java.util.List<JSONObject> batchTariffs = new ArrayList<>();
    private final java.util.List<CheckBox> batchChoices = new ArrayList<>();
    private final java.util.List<Spinner> batchTariffPickers = new ArrayList<>();
    private final java.util.List<JSONObject> batchCards = new ArrayList<>();
    private TextView batchTotalView;
    private EditText batchNotes;
    private RadioGroup batchMethods;
    private Spinner batchCardPicker;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        if (!session.isResidentUi()) {
            toast("Payments on this screen are for your own resident account.");
            finish();
            return;
        }
        bind("Payments");
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (session.isLoggedIn() && session.isResident() && root != null) load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading payments..."));
        String path = statusFilter.isEmpty() ? "api/Payments" : "api/Payments?status=" + statusFilter;
        io(() -> {
            JSONArray payments = ApiClient.getArray(path, session.getToken());
            runOnUiThread(() -> show(payments));
        });
    }

    private void show(JSONArray payments) {
        root.removeAllViews();
        View screen = getLayoutInflater().inflate(R.layout.screen_payments, root, true);

        LinearLayout filters = screen.findViewById(R.id.filterRow);
        filters.removeAllViews();
        addFilter(filters, "All", "");
        addFilter(filters, "Pending", "Pending");
        addFilter(filters, "Confirmed", "Confirmed");
        addFilter(filters, "Failed", "Failed");
        addFilter(filters, "Cancelled", "Cancelled");

        Button pay = screen.findViewById(R.id.payButton);
        pay.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_nav_transact, 0, 0, 0);
        pay.setCompoundDrawablePadding(Ui.dp(this, 10));
        pay.setCompoundDrawableTintList(android.content.res.ColorStateList.valueOf(Ui.onAccent(this)));
        pay.setOnClickListener(v -> showTariffs());
        Button payAll = new Button(this);
        payAll.setText("Pay all services");
        payAll.setAllCaps(false);
        payAll.setTextSize(15);
        payAll.setTypeface(Ui.sansBold());
        payAll.setTextColor(Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY);
        payAll.setMinHeight(Ui.dp(this, 42));
        GradientDrawable payAllShape = new GradientDrawable();
        payAllShape.setColor(Ui.cardColor(this));
        payAllShape.setCornerRadius(Ui.dp(this, 26));
        payAllShape.setStroke(Ui.dp(this, 1), Ui.gold(this));
        payAll.setBackground(payAllShape);
        payAll.setStateListAnimator(null);
        payAll.setElevation(0);
        setButtonIcon(payAll, R.drawable.ic_checkout_money);
        payAll.setCompoundDrawableTintList(android.content.res.ColorStateList.valueOf(Ui.night(this) ? Ui.GOLD_LIGHT : Ui.NAVY));
        payAll.setCompoundDrawablePadding(Ui.dp(this, 8));
        payAll.setOnClickListener(v -> showPayAllServices());
        // Keep the bulk-payment action immediately below the primary payment action.
        // Resolve the actual parent from the inflated view so this remains correct
        // whether the layout is attached directly or through a wrapper container.
        Button payButton = screen.findViewById(R.id.payButton);
        LinearLayout.LayoutParams payAllParams = new LinearLayout.LayoutParams(-1, -2);
        payAllParams.bottomMargin = Ui.dp(this, 14);
        ViewGroup paymentParent = (ViewGroup) payButton.getParent();
        int payIndex = paymentParent.indexOfChild(payButton);
        paymentParent.addView(payAll, payIndex + 1, payAllParams);

        LinearLayout list = screen.findViewById(R.id.paymentList);
        list.removeAllViews();

        if (payments.length() == 0) {
            screen.findViewById(R.id.emptyCard).setVisibility(View.VISIBLE);
            return;
        }

        double pending = 0;
        for (int i = 0; i < payments.length(); i++) {
            JSONObject payment = payments.optJSONObject(i);
            if (payment == null) continue;
            if ("Pending".equalsIgnoreCase(payment.optString("status"))) {
                pending += payment.optDouble("amount", 0);
            }
            int id = payment.optInt("paymentId");
            list.addView(Ui.paymentRow(this, list, payment, () -> openDetail(id)));
        }

        if (statusFilter.isEmpty() && pending > 0) {
            screen.findViewById(R.id.totalCard).setVisibility(View.VISIBLE);
            ((TextView) screen.findViewById(R.id.totalValue)).setText(Format.money(pending));
        }
    }

    private void addFilter(LinearLayout row, String label, String value) {
        boolean selected = value.equals(statusFilter);
        TextView tab = new TextView(this);
        tab.setText(label);
        tab.setGravity(Gravity.CENTER);
        tab.setTextSize(13);
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

        int padH = Ui.dp(this, 12);
        int padV = Ui.dp(this, 8);
        tab.setPadding(padH, padV, padH, padV);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-2, -2);
        params.setMargins(Ui.dp(this, 3), 0, Ui.dp(this, 5), 0);
        tab.setLayoutParams(params);

        tab.setClickable(true);
        tab.setFocusable(true);
        tab.setOnClickListener(v -> {
            statusFilter = value;
            load();
        });
        row.addView(tab);
    }

    private void openDetail(int id) {
        Intent intent = new Intent(this, PaymentDetailActivity.class);
        intent.putExtra("paymentId", id);
        startActivity(intent);
    }

    private void showTariffs() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading payable services..."));
        io(() -> {
            JSONArray tariffs = ApiClient.getArray("api/Payments/tariffs", session.getToken());
            JSONArray vehicles = ApiClient.getArray("api/Vehicles", session.getToken());
            JSONArray cards = ApiClient.getArray("api/Wallet/cards", session.getToken());
            runOnUiThread(() -> {
                Map<String, List<JSONObject>> services = new LinkedHashMap<>();
                boolean canPayParking = vehicles.length() >= 2;
                for (int i = 0; i < tariffs.length(); i++) {
                    JSONObject tariff = tariffs.optJSONObject(i);
                    if (tariff == null) continue;
                    String category = tariff.optString("categoryName");
                    if ("Parking".equalsIgnoreCase(category) && !canPayParking) continue;
                    if (!services.containsKey(category)) services.put(category, new ArrayList<>());
                    services.get(category).add(tariff);
                }

                showServiceStep(services, cards, canPayParking);
            });
        });
    }

    private void showPayAllServices() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading eligible services..."));
        io(() -> {
            JSONArray tariffs = ApiClient.getArray("api/Payments/tariffs", session.getToken());
            JSONArray vehicles = ApiClient.getArray("api/Vehicles", session.getToken());
            JSONArray cards = ApiClient.getArray("api/Wallet/cards", session.getToken());
            runOnUiThread(() -> renderPayAllServices(tariffs, cards, vehicles.length() >= 2));
        });
    }

    private void renderPayAllServices(JSONArray tariffs, JSONArray cards, boolean canPayParking) {
        root.removeAllViews();
        batchTariffs.clear();
        batchChoices.clear();
        batchTariffPickers.clear();
        batchCards.clear();
        addCheckoutProgress(root, 1);
        root.addView(Ui.title(this, "Pay all services"));
        root.addView(Ui.body(this, "Select every service you want to pay now. Each selected service will be submitted as its own pending payment."));

        LinearLayout options = Ui.card(this);
        addIconLabel(options, "Eligible services", R.drawable.ic_checkout_service);
        Map<String, List<JSONObject>> groupedServices = new LinkedHashMap<>();
        for (int i = 0; i < tariffs.length(); i++) {
            JSONObject tariff = tariffs.optJSONObject(i);
            if (tariff == null || ("Parking".equalsIgnoreCase(tariff.optString("categoryName")) && !canPayParking)) continue;
            String service = tariff.optString("categoryName", "Service");
            if (!groupedServices.containsKey(service)) groupedServices.put(service, new ArrayList<>());
            groupedServices.get(service).add(tariff);
        }
        for (Map.Entry<String, List<JSONObject>> entry : groupedServices.entrySet()) {
            List<JSONObject> optionsForService = entry.getValue();
            batchTariffs.add(optionsForService.get(0));
            CheckBox choice = new CheckBox(this);
            choice.setText(entry.getKey());
            choice.setTextColor(Ui.ink(this));
            choice.setMinHeight(Ui.dp(this, 52));
            choice.setPadding(Ui.dp(this, 4), Ui.dp(this, 6), Ui.dp(this, 4), Ui.dp(this, 6));
            choice.setButtonTintList(android.content.res.ColorStateList.valueOf(Ui.green(this)));
            choice.setOnCheckedChangeListener((button, checked) -> updateBatchTotal());
            batchChoices.add(choice);
            LinearLayout serviceRow = new LinearLayout(this);
            serviceRow.setOrientation(LinearLayout.VERTICAL);
            serviceRow.addView(choice, fullWidth());
            List<String> feeLabels = new ArrayList<>();
            for (JSONObject option : optionsForService) {
                feeLabels.add(option.optString("tariffName") + " · " + Format.money(option.optDouble("amount")) +
                        (option.optString("unit").isEmpty() ? "" : " (" + option.optString("unit") + ")"));
            }
            Spinner feePicker = spinner(feeLabels);
            feePicker.setTag(optionsForService);
            batchTariffPickers.add(feePicker);
            feePicker.setVisibility(optionsForService.size() > 1 ? View.VISIBLE : View.GONE);
            feePicker.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
                @Override public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                    updateBatchTotal();
                }
                @Override public void onNothingSelected(AdapterView<?> parent) { updateBatchTotal(); }
            });
            serviceRow.addView(feePicker, fullWidth());
            LinearLayout.LayoutParams rowParams = fullWidth();
            rowParams.bottomMargin = Ui.dp(this, 8);
            options.addView(serviceRow, rowParams);
        }
        root.addView(options);
        if (batchTariffs.isEmpty()) {
            root.addView(Ui.body(this, "There are no eligible services to pay right now."));
            Button back = Ui.button(this, "Back to payments", false);
            back.setOnClickListener(v -> load());
            root.addView(back);
            return;
        }

        LinearLayout total = Ui.card(this);
        addIconLabel(total, "Selected total", R.drawable.ic_checkout_money);
        batchTotalView = Ui.text(this, Format.money(0), 26, Ui.green(this));
        batchTotalView.setTypeface(Ui.sansBold());
        total.addView(batchTotalView);
        root.addView(total);

        batchMethods = new RadioGroup(this);
        batchMethods.setOrientation(RadioGroup.VERTICAL);
        RadioButton eft = paymentMethodChoice("EFT · Pay by bank transfer", R.drawable.ic_checkout_bank);
        RadioButton card = paymentMethodChoice("Card · Use a saved card", R.drawable.ic_nav_card);
        batchMethods.addView(eft);
        batchMethods.addView(card);
        boolean hasCards = cards != null && cards.length() > 0;
        card.setEnabled(hasCards);
        batchMethods.check(hasCards ? card.getId() : eft.getId());
        LinearLayout methodPanel = Ui.card(this);
        addIconLabel(methodPanel, "Payment method", R.drawable.ic_checkout_bank);
        methodPanel.addView(batchMethods, fullWidth());
        root.addView(methodPanel);

        LinearLayout cardPanel = Ui.card(this);
        addIconLabel(cardPanel, "Saved card", R.drawable.ic_nav_card);
        List<String> cardLabels = new ArrayList<>();
        for (int i = 0; i < cards.length(); i++) {
            JSONObject saved = cards.optJSONObject(i);
            if (saved == null) continue;
            batchCards.add(saved);
            cardLabels.add(saved.optString("brand") + " ···· " + saved.optString("last4"));
        }
        batchCardPicker = spinner(cardLabels);
        cardPanel.addView(batchCardPicker, fullWidth());
        root.addView(cardPanel);
        cardPanel.setVisibility(hasCards ? View.VISIBLE : View.GONE);
        batchMethods.setOnCheckedChangeListener((group, id) -> cardPanel.setVisibility(id == card.getId() ? View.VISIBLE : View.GONE));

        batchNotes = Ui.field(this, "Payment notes / description (optional)", InputType.TYPE_CLASS_TEXT);
        LinearLayout notesPanel = Ui.card(this);
        addIconLabel(notesPanel, "Additional details", R.drawable.ic_checkout_service);
        notesPanel.addView(batchNotes, fullWidth());
        root.addView(notesPanel);
        if (!canPayParking) root.addView(Ui.body(this, "Paid parking is excluded until at least two vehicles are registered."));

        Button review = Ui.button(this, "Review selected services", true);
        review.setOnClickListener(v -> reviewPayAll(card.getId()));
        root.addView(review);
        Button cancel = Ui.button(this, "Back", false);
        cancel.setOnClickListener(v -> load());
        root.addView(cancel);
        updateBatchTotal();
    }

    private RadioButton paymentMethodChoice(String label, int icon) {
        RadioButton choice = new RadioButton(this);
        choice.setId(View.generateViewId());
        choice.setText(label);
        choice.setTextColor(Ui.ink(this));
        setCompoundIcon(choice, icon);
        choice.setMinHeight(Ui.dp(this, 48));
        return choice;
    }

    private void updateBatchTotal() {
        double total = 0;
        for (int i = 0; i < batchChoices.size(); i++) {
            if (batchChoices.get(i).isChecked()) total += selectedBatchTariff(i).optDouble("amount");
        }
        if (batchTotalView != null) batchTotalView.setText(Format.money(total));
    }

    private JSONObject selectedBatchTariff(int serviceIndex) {
        List<JSONObject> matching = new ArrayList<>();
        // Spinner options are kept in the same service order; each selected row stores its fee list tag.
        Object tag = batchTariffPickers.get(serviceIndex).getTag();
        if (tag instanceof List<?>) {
            for (Object option : (List<?>) tag) if (option instanceof JSONObject) matching.add((JSONObject) option);
        }
        if (matching.isEmpty()) return batchTariffs.get(serviceIndex);
        int selection = batchTariffPickers.get(serviceIndex).getSelectedItemPosition();
        return matching.get(Math.max(0, Math.min(selection, matching.size() - 1)));
    }

    private void reviewPayAll(int cardId) {
        List<Integer> selected = new ArrayList<>();
        double total = 0;
        LinearLayout summary = Ui.card(this);
        addIconLabel(summary, "Payment summary", R.drawable.ic_checkout_service);
        for (int i = 0; i < batchChoices.size(); i++) {
            if (!batchChoices.get(i).isChecked()) continue;
            selected.add(i);
            JSONObject tariff = selectedBatchTariff(i);
            total += tariff.optDouble("amount");
            Ui.pair(summary, tariff.optString("categoryName"), tariff.optString("tariffName") + " · " + Format.money(tariff.optDouble("amount")));
        }
        if (selected.isEmpty()) {
            toast("Select at least one service to continue.");
            return;
        }
        String method = batchMethods.getCheckedRadioButtonId() == cardId ? "Card" : "EFT";
        if ("Card".equals(method) && batchCards.isEmpty()) {
            toast("Add a saved card before continuing.");
            return;
        }
        Ui.pair(summary, "Payment method", method);
        TextView totalLabel = Ui.text(this, "Total  " + Format.money(total), 22, Ui.green(this));
        totalLabel.setTypeface(Ui.sansBold());
        summary.addView(totalLabel);
        Ui.confirmation(this, "Review all services", "Campus Estate Living will create one pending payment for each selected service.")
                .setView(summary)
                .setNegativeButton("Review selection", null)
                .setPositiveButton("Submit all", (dialog, which) -> submitBatch(selected, method))
                .show();
    }

    private void submitBatch(List<Integer> selected, String method) {
        int savedCardId = 0;
        if ("Card".equals(method)) {
            int index = batchCardPicker.getSelectedItemPosition();
            if (index < 0 || index >= batchCards.size()) {
                toast("Choose a saved card.");
                return;
            }
            savedCardId = batchCards.get(index).optInt("cardId");
        }
        final int cardId = savedCardId;
        final String notes = batchNotes.getText().toString().trim();
        root.removeAllViews();
        root.addView(Ui.body(this, "Submitting selected service payments..."));
        io(() -> {
            List<String> references = new ArrayList<>();
            Exception failure = null;
            for (int selectedIndex : selected) {
                try {
                    JSONObject body = new JSONObject();
                    body.put("tariffId", selectedBatchTariff(selectedIndex).optInt("tariffId"));
                    body.put("paymentMethod", method);
                    if (cardId > 0) body.put("savedCardId", cardId);
                    if (!notes.isEmpty()) body.put("notes", notes);
                    JSONObject created = ApiClient.post("api/Payments", body, session.getToken());
                    references.add(created.optString("referenceNumber", "submitted"));
                } catch (Exception error) {
                    failure = error;
                    break;
                }
            }
            final Exception batchFailure = failure;
            runOnUiThread(() -> {
                if (batchFailure != null) {
                    String partial = references.isEmpty() ? "No payments were submitted." : references.size() + " payment(s) were submitted before the issue. References: " + android.text.TextUtils.join(", ", references);
                    Ui.confirmation(this, "Batch stopped", partial + "\n\n" + batchFailure.getMessage())
                            .setPositiveButton("View payments", (dialog, which) -> load())
                            .show();
                } else {
                    Ui.confirmation(this, "Payments submitted", references.size() + " services were submitted to Campus Estate Living. Each payment is pending financial officer review.\nReferences: " + android.text.TextUtils.join(", ", references))
                            .setCancelable(false)
                            .setPositiveButton("Done", (dialog, which) -> load())
                            .show();
                }
            });
        });
    }

    private void showServiceStep(Map<String, List<JSONObject>> services, JSONArray cards, boolean canPayParking) {
        root.removeAllViews();
        addCheckoutProgress(root, 1);
        root.addView(Ui.title(this, "Choose a service"));
        root.addView(Ui.body(this, "Select a service and the matching fee or usage option."));

        if (services.isEmpty()) {
            root.addView(Ui.body(this, "No payable services are available right now."));
            Button back = Ui.button(this, "Back to payments", false);
            back.setOnClickListener(v -> load());
            root.addView(back);
            return;
        }

        List<String> serviceNames = new ArrayList<>(services.keySet());
        Spinner servicePicker = spinner(serviceNames);
        Spinner tariffPicker = spinner(new ArrayList<>());
        LinearLayout selection = Ui.card(this);
        addIconLabel(selection, "Service", R.drawable.ic_checkout_service);
        selection.addView(servicePicker, fullWidth());
        addIconLabel(selection, "Fee or usage option", R.drawable.ic_nav_transact);
        selection.addView(tariffPicker, fullWidth());
        root.addView(selection);

        LinearLayout amountCard = Ui.card(this);
        addIconLabel(amountCard, "Amount due", R.drawable.ic_checkout_money);
        TextView amountView = Ui.text(this, "Choose an option", 24, Ui.green(this));
        amountView.setTypeface(Ui.sansBold());
        amountView.setPadding(0, Ui.dp(this, 4), 0, 0);
        amountCard.addView(amountView);
        root.addView(amountCard);

        EditText notes = Ui.field(this, "Payment notes / description (optional)", InputType.TYPE_CLASS_TEXT);
        notes.setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_checkout_service, 0, 0, 0);
        notes.setCompoundDrawablePadding(Ui.dp(this, 10));
        LinearLayout notesCard = Ui.card(this);
        addIconLabel(notesCard, "Additional details", R.drawable.ic_checkout_service);
        notesCard.addView(notes, fullWidth());
        root.addView(notesCard);
        if (!canPayParking) {
            root.addView(Ui.body(this, "Paid parking becomes available after registering at least two vehicles."));
        }

        servicePicker.setOnItemSelectedListener(new android.widget.AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(android.widget.AdapterView<?> parent, View view, int position, long id) {
                List<JSONObject> options = services.get(serviceNames.get(position));
                List<String> labels = new ArrayList<>();
                for (JSONObject option : options) {
                    labels.add(option.optString("tariffName") + " · " + Format.money(option.optDouble("amount")) +
                            (option.optString("unit").isEmpty() ? "" : " (" + option.optString("unit") + ")"));
                }
                tariffPicker.setAdapter(spinnerAdapter(labels));
                if (options.isEmpty()) amountView.setText("No active options");
            }

            @Override
            public void onNothingSelected(android.widget.AdapterView<?> parent) { }
        });
        tariffPicker.setOnItemSelectedListener(new android.widget.AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(android.widget.AdapterView<?> parent, View view, int position, long id) {
                int selectedService = servicePicker.getSelectedItemPosition();
                if (selectedService < 0 || selectedService >= serviceNames.size()) return;
                List<JSONObject> options = services.get(serviceNames.get(selectedService));
                if (options != null && position >= 0 && position < options.size()) {
                    amountView.setText(Format.money(options.get(position).optDouble("amount")));
                }
            }

            @Override
            public void onNothingSelected(android.widget.AdapterView<?> parent) {
                amountView.setText("Choose an option");
            }
        });

        Button next = Ui.button(this, "Continue to payment method", true);
        setButtonIcon(next, R.drawable.ic_checkout_forward);
        next.setOnClickListener(v -> {
            int serviceIndex = servicePicker.getSelectedItemPosition();
            List<JSONObject> options = services.get(serviceNames.get(serviceIndex));
            int tariffIndex = tariffPicker.getSelectedItemPosition();
            if (options == null || options.isEmpty() || tariffIndex < 0 || tariffIndex >= options.size()) {
                toast("Choose an available service option.");
                return;
            }
            showMethodStep(serviceNames.get(serviceIndex), options.get(tariffIndex), notes.getText().toString().trim(), cards);
        });
        root.addView(next);
        Button cancel = Ui.button(this, "Cancel", false);
        setButtonIcon(cancel, R.drawable.ic_checkout_back);
        cancel.setOnClickListener(v -> load());
        root.addView(cancel);
    }

    private void showMethodStep(String serviceName, JSONObject tariff, String notes, JSONArray cards) {
        root.removeAllViews();
        addCheckoutProgress(root, 2);
        root.addView(Ui.title(this, "Choose a payment method"));
        root.addView(Ui.body(this, "Select how you will pay. Every payment stays pending until reviewed."));

        LinearLayout summary = Ui.card(this);
        addIconLabel(summary, "Payment summary", R.drawable.ic_checkout_service);
        Ui.pair(summary, "Service", serviceName);
        Ui.pair(summary, "Option", tariff.optString("tariffName"));
        Ui.pair(summary, "Amount due", Format.money(tariff.optDouble("amount")));
        root.addView(summary);

        RadioGroup methods = new RadioGroup(this);
        methods.setOrientation(RadioGroup.VERTICAL);
        RadioButton eft = new RadioButton(this);
        eft.setId(View.generateViewId());
        eft.setText("EFT · Pay by bank transfer");
        eft.setTextColor(Ui.ink(this));
        setCompoundIcon(eft, R.drawable.ic_checkout_bank);
        methods.addView(eft);
        RadioButton card = new RadioButton(this);
        card.setId(View.generateViewId());
        card.setText("Card · Submit a saved card for review");
        card.setTextColor(Ui.ink(this));
        setCompoundIcon(card, R.drawable.ic_nav_card);
        methods.addView(card);
        List<JSONObject> savedCards = new ArrayList<>();
        List<String> cardLabels = new ArrayList<>();
        int defaultIndex = 0;
        for (int i = 0; i < cards.length(); i++) {
            JSONObject savedCard = cards.optJSONObject(i);
            if (savedCard == null) continue;
            if (savedCard.optBoolean("isDefault")) defaultIndex = savedCards.size();
            savedCards.add(savedCard);
            cardLabels.add(savedCard.optString("brand") + " ···· " + savedCard.optString("last4") +
                    "  (" + String.format(java.util.Locale.ROOT, "%02d/%04d",
                    savedCard.optInt("expiryMonth"), savedCard.optInt("expiryYear")) + ")");
        }
        boolean hasSavedCards = !savedCards.isEmpty();
        methods.check(hasSavedCards ? card.getId() : eft.getId());
        LinearLayout methodsCard = Ui.card(this);
        addIconLabel(methodsCard, "Payment method", R.drawable.ic_checkout_bank);
        methodsCard.addView(methods, fullWidth());
        root.addView(methodsCard);

        LinearLayout cardPanel = Ui.card(this);
        Spinner cardPicker = null;
        if (hasSavedCards) {
            cardPicker = spinner(cardLabels);
            cardPicker.setSelection(Math.min(defaultIndex, Math.max(0, savedCards.size() - 1)));
            addIconLabel(cardPanel, "Saved card", R.drawable.ic_nav_card);
            cardPanel.addView(cardPicker);
        } else {
            addIconLabel(cardPanel, "Card payment", R.drawable.ic_nav_card);
            cardPanel.addView(Ui.body(this, "No saved cards yet. Add one to submit this payment by card."));
            Button addCard = Ui.button(this, "Manage payment methods", false);
            setButtonIcon(addCard, R.drawable.ic_nav_card);
            addCard.setOnClickListener(v -> open(WalletActivity.class));
            cardPanel.addView(addCard);
        }
        root.addView(cardPanel);
        cardPanel.setVisibility(hasSavedCards ? View.VISIBLE : View.GONE);

        LinearLayout bank = Ui.card(this);
        addIconLabel(bank, "Estate EFT details", R.drawable.ic_checkout_bank);
        bank.addView(Ui.body(this, "First National Bank · UFS QwaQwa Estate\nAccount 62841029331 · Branch 250655\nUse your estate account number as the reference. Your payment will remain pending until verified."));
        root.addView(bank);
        bank.setVisibility(hasSavedCards ? View.GONE : View.VISIBLE);

        RadioButton selectedCardPicker = card;
        Spinner selectedCardSpinner = cardPicker;
        methods.setOnCheckedChangeListener((group, checkedId) -> {
            boolean cardSelected = checkedId == selectedCardPicker.getId();
            cardPanel.setVisibility(cardSelected ? View.VISIBLE : View.GONE);
            bank.setVisibility(cardSelected ? View.GONE : View.VISIBLE);
        });

        Button review = Ui.button(this, "Review payment", true);
        setButtonIcon(review, R.drawable.ic_checkout_check);
        review.setOnClickListener(v -> {
            boolean cardSelected = methods.getCheckedRadioButtonId() == card.getId();
            int savedCardId = 0;
            String cardLabel = "";
            if (cardSelected) {
                if (!hasSavedCards) {
                    toast("Add a saved card before continuing.");
                    return;
                }
                int cardIndex = selectedCardSpinner.getSelectedItemPosition();
                if (cardIndex < 0 || cardIndex >= savedCards.size()) {
                    toast("Choose a saved card.");
                    return;
                }
                JSONObject savedCard = savedCards.get(cardIndex);
                if (savedCard == null) {
                    toast("Choose a saved card.");
                    return;
                }
                savedCardId = savedCard.optInt("cardId");
                cardLabel = savedCard.optString("brand") + " ···· " + savedCard.optString("last4");
            }
            confirmPayment(serviceName, tariff, notes, cardSelected ? "Card" : "EFT", savedCardId, cardLabel, review);
        });
        root.addView(review);
        Button back = Ui.button(this, "Change service", false);
        setButtonIcon(back, R.drawable.ic_checkout_back);
        back.setOnClickListener(v -> showTariffs());
        root.addView(back);
    }

    private void confirmPayment(String serviceName, JSONObject tariff, String notes, String method, int savedCardId, String cardLabel, Button reviewButton) {
        LinearLayout reviewContent = new LinearLayout(this);
        reviewContent.setOrientation(LinearLayout.VERTICAL);
        reviewContent.setPadding(Ui.dp(this, 4), Ui.dp(this, 8), Ui.dp(this, 4), Ui.dp(this, 2));
        addCheckoutProgress(reviewContent, 3);

        LinearLayout reviewSummary = Ui.card(this);
        addIconLabel(reviewSummary, "Payment summary", R.drawable.ic_checkout_service);
        Ui.pair(reviewSummary, "Service", serviceName);
        Ui.pair(reviewSummary, "Option", tariff.optString("tariffName"));
        Ui.pair(reviewSummary, "Payment method", "Card".equals(method) ? "Card · " + cardLabel : "EFT · Bank transfer");
        TextView amount = Ui.text(this, Format.money(tariff.optDouble("amount")), 28, Ui.green(this));
        amount.setTypeface(Ui.sansBold());
        amount.setPadding(0, Ui.dp(this, 14), 0, Ui.dp(this, 4));
        reviewSummary.addView(amount);
        reviewContent.addView(reviewSummary);

        LinearLayout pendingNotice = Ui.card(this);
        addIconLabel(pendingNotice, "Awaiting confirmation", R.drawable.ic_checkout_check);
        pendingNotice.addView(Ui.body(this, "No funds are transferred now. A financial officer must review and confirm this payment."));
        reviewContent.addView(pendingNotice);

        AlertDialog dialog = Ui.confirmation(this, "Review and confirm", "Check the payment details below before submitting.")
                .setView(reviewContent)
                .create();
        dialog.setButton(AlertDialog.BUTTON_NEGATIVE, "Go back", (ignored, which) -> dialog.dismiss());
        dialog.setButton(AlertDialog.BUTTON_POSITIVE, "Submit payment", (ignored, which) -> submit(tariff.optInt("tariffId"), notes, method, savedCardId, reviewButton));
        dialog.setOnShowListener(ignored -> {
            setDialogButtonIcon(dialog.getButton(AlertDialog.BUTTON_POSITIVE), R.drawable.ic_checkout_forward);
            setDialogButtonIcon(dialog.getButton(AlertDialog.BUTTON_NEGATIVE), R.drawable.ic_checkout_back);
        });
        dialog.show();
    }

    private void submit(int tariffId, String notes, String method, int savedCardId, Button submitButton) {
        submitButton.setEnabled(false);
        submitButton.setText("Submitting...");
        io(() -> {
            try {
                JSONObject body = new JSONObject();
                body.put("tariffId", tariffId);
                body.put("paymentMethod", method);
                if (savedCardId > 0) body.put("savedCardId", savedCardId);
                if (!notes.isEmpty()) body.put("notes", notes);
                JSONObject created = ApiClient.post("api/Payments", body, session.getToken());
                runOnUiThread(() -> showPaymentConfirmation(created));
            } catch (Exception error) {
                runOnUiThread(() -> {
                    submitButton.setEnabled(true);
                    submitButton.setText("Review payment");
                    onError(error);
                });
            }
        });
    }

    private void showPaymentConfirmation(JSONObject payment) {
        String reference = payment.optString("referenceNumber", "Payment received");
        String status = payment.optString("status", "Pending");
        AlertDialog dialog = Ui.confirmation(this, "Payment submitted", "Reference: " + reference + "\nStatus: " + status +
                        "\n\nYour payment is awaiting financial officer verification. No funds are transferred before confirmation.")
                .setCancelable(false)
                .create();
        dialog.setButton(AlertDialog.BUTTON_POSITIVE, "View payment", (ignored, which) -> openDetail(payment.optInt("paymentId")));
        dialog.setButton(AlertDialog.BUTTON_NEGATIVE, "Done", (ignored, which) -> load());
        dialog.setIcon(R.drawable.ic_checkout_check);
        dialog.setOnShowListener(ignored -> {
            setDialogButtonIcon(dialog.getButton(AlertDialog.BUTTON_POSITIVE), R.drawable.ic_checkout_service);
            setDialogButtonIcon(dialog.getButton(AlertDialog.BUTTON_NEGATIVE), R.drawable.ic_checkout_check);
        });
        dialog.show();
    }

    private void addCheckoutProgress(LinearLayout parent, int activeStep) {
        LinearLayout progress = new LinearLayout(this);
        progress.setOrientation(LinearLayout.HORIZONTAL);
        progress.setGravity(Gravity.CENTER_VERTICAL);
        progress.setPadding(0, Ui.dp(this, 4), 0, Ui.dp(this, 14));
        int[] icons = {R.drawable.ic_checkout_service, R.drawable.ic_nav_card, R.drawable.ic_checkout_check};
        String[] labels = {"Service", "Method", "Review"};

        for (int i = 0; i < labels.length; i++) {
            final int step = i + 1;
            LinearLayout item = new LinearLayout(this);
            item.setOrientation(LinearLayout.VERTICAL);
            item.setGravity(Gravity.TOP | Gravity.CENTER_HORIZONTAL);

            FrameLayout iconBubble = new FrameLayout(this);
            GradientDrawable bubble = new GradientDrawable();
            bubble.setShape(GradientDrawable.OVAL);
            boolean reached = step <= activeStep;
            bubble.setColor(reached ? (Ui.night(this) ? 0x33D4AF37 : 0x1823415B) : Ui.cardColor(this));
            bubble.setStroke(Ui.dp(this, 1), reached ? Ui.gold(this) : Ui.line(this));
            iconBubble.setBackground(bubble);
            ImageView icon = new ImageView(this);
            icon.setImageResource(icons[i]);
            Drawable iconDrawable = icon.getDrawable();
            if (iconDrawable != null) iconDrawable.setTint(reached ? Ui.green(this) : Ui.muted(this));
            FrameLayout.LayoutParams iconParams = new FrameLayout.LayoutParams(Ui.dp(this, 20), Ui.dp(this, 20), Gravity.CENTER);
            iconBubble.addView(icon, iconParams);
            LinearLayout.LayoutParams bubbleParams = new LinearLayout.LayoutParams(Ui.dp(this, 38), Ui.dp(this, 38));
            bubbleParams.gravity = Gravity.CENTER_HORIZONTAL;
            item.addView(iconBubble, bubbleParams);

            TextView label = Ui.text(this, labels[i], 11, step == activeStep ? Ui.green(this) : Ui.muted(this));
            label.setTypeface(step == activeStep ? Ui.sansBold() : Ui.sansMedium());
            label.setPadding(0, Ui.dp(this, 5), 0, 0);
            label.setGravity(Gravity.CENTER);
            label.setMaxLines(1);
            label.setIncludeFontPadding(false);
            label.setMinWidth(Ui.dp(this, 56));
            item.addView(label, new LinearLayout.LayoutParams(-1, -2));
            progress.addView(item, new LinearLayout.LayoutParams(0, -2, 1f));
        }
        parent.addView(progress);
    }

    private void addIconLabel(LinearLayout parent, String label, int iconResource) {
        TextView view = Ui.text(this, label, 14, Ui.green(this));
        view.setTypeface(Ui.sansBold());
        view.setPadding(0, 0, 0, Ui.dp(this, 8));
        Drawable icon = getDrawable(iconResource);
        if (icon != null) icon.setTint(Ui.green(this));
        view.setCompoundDrawablesRelativeWithIntrinsicBounds(icon, null, null, null);
        view.setCompoundDrawablePadding(Ui.dp(this, 9));
        parent.addView(view);
    }

    private void setCompoundIcon(TextView view, int iconResource) {
        Drawable icon = getDrawable(iconResource);
        if (icon != null) icon.setTint(Ui.green(this));
        view.setCompoundDrawablesRelativeWithIntrinsicBounds(icon, null, null, null);
        view.setCompoundDrawablePadding(Ui.dp(this, 10));
    }

    private void setButtonIcon(Button button, int iconResource) {
        setDialogButtonIcon(button, iconResource);
    }

    private void setDialogButtonIcon(android.widget.Button button, int iconResource) {
        if (button == null) return;
        Drawable icon = getDrawable(iconResource);
        if (icon != null) icon.setTint(button.getCurrentTextColor());
        button.setCompoundDrawablesRelativeWithIntrinsicBounds(icon, null, null, null);
        button.setCompoundDrawablePadding(Ui.dp(this, 8));
    }

    private Spinner spinner(List<String> values) {
        Spinner spinner = new Spinner(this);
        spinner.setAdapter(spinnerAdapter(values));
        spinner.setPadding(Ui.dp(this, 8), Ui.dp(this, 8), Ui.dp(this, 8), Ui.dp(this, 8));
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = Ui.dp(this, 12);
        spinner.setLayoutParams(params);
        return spinner;
    }

    private LinearLayout.LayoutParams fullWidth() {
        return new LinearLayout.LayoutParams(-1, -2);
    }

    private ArrayAdapter<String> spinnerAdapter(List<String> values) {
        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, values);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        return adapter;
    }
}
