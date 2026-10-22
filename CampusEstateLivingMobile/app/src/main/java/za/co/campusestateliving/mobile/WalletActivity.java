package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.os.Bundle;
import android.text.InputType;
import android.view.Gravity;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.Spinner;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

/** Saved cards and banks. The full card number is sent once and is not stored on the phone. */
public class WalletActivity extends CelActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (!gate()) return;
        if (!session.isResidentUi()) {
            bind(null);
            root.addView(Ui.body(this, "Saved cards and bank accounts belong to a resident profile, not an officer account."));
            return;
        }
        bind(null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading wallet..."));
        io(() -> {
            JSONArray cards = ApiClient.getArray("api/Wallet/cards", session.getToken());
            JSONArray banks = ApiClient.getArray("api/Wallet/banks", session.getToken());
            runOnUiThread(() -> show(cards, banks));
        });
    }

    private void show(JSONArray cards, JSONArray banks) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "Digital Wallet"));
        root.addView(Ui.title(this, "Payment methods"));

        // Information banner
        LinearLayout securityNotice = Ui.card(this);
        securityNotice.addView(Ui.text(this, "Bank-grade tokenization", 15, Ui.green(this)));
        securityNotice.addView(Ui.body(this, "Card details are encrypted on transit. Only the card brand, last four digits, and expiry dates are stored for display."));
        root.addView(securityNotice);

        // Saved Cards Section
        root.addView(Ui.text(this, "Saved cards", 18, Ui.green(this)));
        if (cards.length() == 0) {
            LinearLayout emptyCard = Ui.card(this);
            emptyCard.addView(Ui.body(this, "No saved payment cards yet. Add your debit or credit card below."));
            root.addView(emptyCard);
        } else {
            for (int i = 0; i < cards.length(); i++) {
                JSONObject card = cards.optJSONObject(i);
                if (card == null) continue;
                String brand = card.optString("brand", "Card");
                String last4 = card.optString("last4", "••••");
                String cardholder = card.optString("cardholderName", "CARDHOLDER");
                String exp = String.format("%02d/%s", card.optInt("expiryMonth", 1), String.valueOf(card.optInt("expiryYear", 28)).substring(Math.max(0, String.valueOf(card.optInt("expiryYear", 28)).length() - 2)));
                root.addView(Ui.creditCardWidget(this, brand, last4, cardholder,
                        card.optInt("expiryMonth", 1), card.optInt("expiryYear", 2028)));
                int cardId = card.optInt("cardId");
                Button removeCard = Ui.button(this, "Remove " + brand + " •••• " + last4, false);
                removeCard.setOnClickListener(v -> io(() -> {
                    ApiClient.delete("api/Wallet/cards/" + cardId, session.getToken());
                    runOnUiThread(() -> {
                        toast("Card removed.");
                        load();
                    });
                }));
                root.addView(removeCard);
            }
        }

        // Add Card Form Card
        LinearLayout addCardBox = Ui.card(this);
        addCardBox.addView(Ui.text(this, "Add new card", 16, Ui.green(this)));
        EditText holder = Ui.field(this, "Cardholder name", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PERSON_NAME);
        EditText number = Ui.field(this, "Card number (16 digits)", InputType.TYPE_CLASS_NUMBER);

        LinearLayout expRow = new LinearLayout(this);
        expRow.setOrientation(LinearLayout.HORIZONTAL);
        EditText month = Ui.field(this, "Expiry month (MM)", InputType.TYPE_CLASS_NUMBER);
        EditText year = Ui.field(this, "Expiry year (YYYY)", InputType.TYPE_CLASS_NUMBER);
        LinearLayout.LayoutParams p1 = new LinearLayout.LayoutParams(0, -2, 1f);
        p1.setMargins(0, 0, Ui.dp(this, 6), 0);
        month.setLayoutParams(p1);
        LinearLayout.LayoutParams p2 = new LinearLayout.LayoutParams(0, -2, 1f);
        p2.setMargins(Ui.dp(this, 6), 0, 0, 0);
        year.setLayoutParams(p2);
        expRow.addView(month);
        expRow.addView(year);

        addCardBox.addView(holder);
        addCardBox.addView(number);
        addCardBox.addView(expRow);

        Button addCard = Ui.button(this, "Save card securely", true);
        addCard.setOnClickListener(v -> {
            String cardholder = holder.getText().toString().trim();
            String cardNumber = number.getText().toString().trim();
            String expiryMonth = month.getText().toString().trim();
            String expiryYear = year.getText().toString().trim();
            if (cardholder.isEmpty() || cardNumber.isEmpty() || expiryMonth.isEmpty() || expiryYear.isEmpty()) {
                toast("Please fill in all card details.");
                return;
            }
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("cardholderName", cardholder);
                body.put("cardNumber", cardNumber);
                body.put("expiryMonth", Integer.parseInt(expiryMonth));
                body.put("expiryYear", Integer.parseInt(expiryYear));
                ApiClient.post("api/Wallet/cards", body, session.getToken());
                runOnUiThread(() -> {
                    number.setText("");
                    toast("Card saved successfully.");
                    load();
                });
            });
        });
        addCardBox.addView(addCard);
        root.addView(addCardBox);

        // Saved Banks Section
        root.addView(Ui.text(this, "Saved bank accounts", 18, Ui.green(this)));
        if (banks.length() == 0) {
            LinearLayout emptyBank = Ui.card(this);
            emptyBank.addView(Ui.body(this, "No saved bank accounts."));
            root.addView(emptyBank);
        } else {
            for (int i = 0; i < banks.length(); i++) {
                JSONObject bank = banks.optJSONObject(i);
                if (bank == null) continue;
                LinearLayout row = Ui.card(this);
                TextView bName = Ui.text(this, bank.optString("bankName") + "  •••• " + bank.optString("accountNumberLast4"), 16, Ui.green(this));
                bName.setTypeface(Ui.sansBold());
                row.addView(bName);
                row.addView(Ui.body(this, bank.optString("accountHolder") + "  ·  " + bank.optString("accountType") + " account  ·  Branch: " + bank.optString("branchCode")));
                int bankId = bank.optInt("bankAccountId");
                Button removeBank = Ui.button(this, "Remove bank account", false);
                removeBank.setOnClickListener(v -> io(() -> {
                    ApiClient.delete("api/Wallet/banks/" + bankId, session.getToken());
                    runOnUiThread(() -> {
                        toast("Bank account removed.");
                        load();
                    });
                }));
                row.addView(removeBank);
                root.addView(row);
            }
        }

        // Add Bank Account Form Card
        LinearLayout addBankBox = Ui.card(this);
        addBankBox.addView(Ui.text(this, "Add bank account for EFT", 16, Ui.green(this)));

        Spinner bankName = spinner(new String[]{
                "First National Bank", "Standard Bank", "ABSA", "Nedbank", "Capitec Bank", "Investec", "Discovery Bank", "African Bank"
        });
        Spinner accountType = spinner(new String[]{"Cheque / Current", "Savings"});
        EditText accountHolder = Ui.field(this, "Account holder name", InputType.TYPE_CLASS_TEXT);
        EditText accountNumber = Ui.field(this, "Account number", InputType.TYPE_CLASS_NUMBER);
        EditText branch = Ui.field(this, "Branch code", InputType.TYPE_CLASS_NUMBER);

        addBankBox.addView(Ui.text(this, "Financial institution", 13, Ui.muted(this)));
        addBankBox.addView(bankName);
        addBankBox.addView(accountHolder);
        addBankBox.addView(accountNumber);
        addBankBox.addView(branch);
        addBankBox.addView(Ui.text(this, "Account type", 13, Ui.muted(this)));
        addBankBox.addView(accountType);

        Button addBank = Ui.button(this, "Save bank account", false);
        addBank.setOnClickListener(v -> {
            String selectedBank = String.valueOf(bankName.getSelectedItem());
            String holderName = accountHolder.getText().toString().trim();
            String digits = accountNumber.getText().toString().trim();
            String branchCode = branch.getText().toString().trim();
            String type = String.valueOf(accountType.getSelectedItem());
            if (holderName.isEmpty() || digits.isEmpty() || branchCode.isEmpty()) {
                toast("Please complete all bank fields.");
                return;
            }
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("bankName", selectedBank);
                body.put("accountHolder", holderName);
                body.put("accountNumber", digits);
                body.put("branchCode", branchCode);
                body.put("accountType", type);
                ApiClient.post("api/Wallet/banks", body, session.getToken());
                runOnUiThread(() -> {
                    accountNumber.setText("");
                    toast("Bank account saved successfully.");
                    load();
                });
            });
        });
        addBankBox.addView(addBank);
        root.addView(addBankBox);
    }

    private Spinner spinner(String[] values) {
        Spinner spinner = new Spinner(this);
        spinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, values));
        spinner.setPadding(Ui.dp(this, 8), Ui.dp(this, 10), Ui.dp(this, 8), Ui.dp(this, 10));
        return spinner;
    }
}