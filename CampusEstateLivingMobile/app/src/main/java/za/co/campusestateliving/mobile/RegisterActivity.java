package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.os.Bundle;
import android.text.InputType;
import android.text.method.PasswordTransformationMethod;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.Spinner;

import org.json.JSONObject;

import java.util.Calendar;

import za.co.campusestateliving.mobile.model.LoginResponse;

/** Public registration matches api/Auth/register: Student or Staff only. */
public class RegisterActivity extends CelActivity {
    private EditText firstName;
    private EditText lastName;
    private Spinner accountType;
    private EditText userNumber;
    private EditText dob;
    private Spinner gender;
    private CheckBox disability;
    private EditText disabilityDetails;
    private EditText phone;
    private EditText email;
    private EditText password;
    private EditText confirm;
    private Button submit;
    private boolean busy;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        bind(null);
        root.addView(Ui.eyebrow(this, "Campus Estate Living"));
        root.addView(Ui.title(this, "Create account"));
        root.addView(Ui.body(this, "Register as a student or staff member to access your estate account."));

        LinearLayout identity = formCard("Personal details", "Enter your name as it appears on your identification.");
        LinearLayout nameRow = new LinearLayout(this);
        nameRow.setOrientation(LinearLayout.HORIZONTAL);
        firstName = formInput(identity, "First name", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PERSON_NAME);
        lastName = formInput(identity, "Last name", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PERSON_NAME);
        LinearLayout.LayoutParams firstParams = new LinearLayout.LayoutParams(0, -2, 1f);
        firstParams.rightMargin = Ui.dp(this, 6);
        firstName.setLayoutParams(firstParams);
        LinearLayout.LayoutParams lastParams = new LinearLayout.LayoutParams(0, -2, 1f);
        lastParams.leftMargin = Ui.dp(this, 6);
        lastName.setLayoutParams(lastParams);
        identity.removeView(firstName);
        identity.removeView(lastName);
        nameRow.addView(firstName);
        nameRow.addView(lastName);
        identity.addView(nameRow);

        LinearLayout account = formCard("Account type", "Use your student or staff identification number.");
        accountType = formSpinner(account, "I am a", new String[]{"Student", "Staff"});
        userNumber = formInput(account, "Student number (10 digits) or staff number (7 digits)", InputType.TYPE_CLASS_NUMBER);
        accountType.setOnItemSelectedListener(new android.widget.AdapterView.OnItemSelectedListener() {
            @Override public void onItemSelected(android.widget.AdapterView<?> parent, android.view.View view, int position, long id) {
                userNumber.setHint(position == 0 ? "Student number (10 digits)" : "Staff number (7 digits)");
            }
            @Override public void onNothingSelected(android.widget.AdapterView<?> parent) {}
        });

        LinearLayout contact = formCard("Contact details", "We use these details for account updates.");
        phone = formInput(contact, "Phone number", InputType.TYPE_CLASS_PHONE);
        email = formInput(contact, "Email address", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS);

        LinearLayout profile = formCard("Optional details", "You can add these now or leave them blank.");
        dob = formInput(profile, "Date of birth (yyyy-mm-dd)", InputType.TYPE_CLASS_DATETIME | InputType.TYPE_DATETIME_VARIATION_DATE);
        gender = formSpinner(profile, "Gender", new String[]{"", "Female", "Male", "Non-binary", "Prefer not to say"});
        disability = new CheckBox(this);
        disability.setText("I have a disability");
        disability.setTextColor(Ui.ink(this));
        profile.addView(disability);
        disabilityDetails = formInput(profile, "Disability details (optional)", InputType.TYPE_CLASS_TEXT);
        disabilityDetails.setVisibility(android.view.View.GONE);
        disability.setOnCheckedChangeListener((button, checked) -> disabilityDetails.setVisibility(checked ? android.view.View.VISIBLE : android.view.View.GONE));

        LinearLayout security = formCard("Create a password", "Use at least 8 characters with uppercase, lowercase, a number, and a symbol.");
        password = formInput(security, "Password", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        password.setTransformationMethod(PasswordTransformationMethod.getInstance());
        confirm = formInput(security, "Confirm password", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        confirm.setTransformationMethod(PasswordTransformationMethod.getInstance());

        root.addView(identity);
        root.addView(account);
        root.addView(contact);
        root.addView(profile);
        root.addView(security);

        submit = Ui.button(this, "Create account", true);
        submit.setOnClickListener(v -> submit());
        root.addView(submit);
        Button back = Ui.button(this, "Back to sign in", false);
        back.setOnClickListener(v -> finish());
        root.addView(back);
    }

    private LinearLayout formCard(String title, String description) {
        LinearLayout card = Ui.card(this);
        android.widget.TextView heading = Ui.text(this, title, 17, Ui.ink(this));
        heading.setTypeface(Ui.sansBold());
        card.addView(heading);
        android.widget.TextView caption = Ui.text(this, description, 12, Ui.muted(this));
        caption.setPadding(0, Ui.dp(this, 3), 0, Ui.dp(this, 10));
        card.addView(caption);
        return card;
    }

    private EditText formInput(LinearLayout parent, String hint, int type) {
        EditText field = Ui.field(this, hint, type);
        parent.addView(field);
        return field;
    }

    private Spinner formSpinner(LinearLayout parent, String label, String[] values) {
        android.widget.TextView caption = Ui.text(this, label, 12, Ui.muted(this));
        caption.setTypeface(Ui.sansBold());
        caption.setPadding(0, Ui.dp(this, 4), 0, Ui.dp(this, 4));
        parent.addView(caption);
        Spinner spinner = new Spinner(this);
        spinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, values));
        spinner.setMinimumHeight(Ui.dp(this, 48));
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = Ui.dp(this, 12);
        spinner.setLayoutParams(params);
        parent.addView(spinner);
        return spinner;
    }

    private void submit() {
        if (busy) return;
        String type = String.valueOf(accountType.getSelectedItem());
        String number = userNumber.getText().toString().trim();
        int expected = "Student".equals(type) ? 10 : 7;
        if (!number.matches("\\d{" + expected + "}")) {
            toast((("Student".equals(type) ? "Student" : "Staff") + " number must contain exactly " + expected + " digits."));
            return;
        }
        String pass = password.getText().toString();
        if (!pass.equals(confirm.getText().toString())) {
            toast("Password and confirmation do not match.");
            return;
        }
        if (!strong(pass)) {
            toast("Password must be 8 or more characters and include upper, lower, a number and a symbol.");
            return;
        }
        String birth = dob.getText().toString().trim();
        if (!birth.isEmpty() && !validBirth(birth)) {
            toast("Enter a valid date of birth as yyyy-mm-dd.");
            return;
        }
        if (firstName.getText().toString().trim().isEmpty() || lastName.getText().toString().trim().isEmpty() || email.getText().toString().trim().isEmpty()) {
            toast("First name, last name and email are required.");
            return;
        }
        Ui.confirmation(this, "Create your account?", "Review your details before creating your Campus Estate Living account.")
                .setNegativeButton("Review details", null)
                .setPositiveButton("Create account", (dialog, which) -> createAccount(type, number, pass, birth))
                .show();
    }

    private void createAccount(String type, String number, String pass, String birth) {
        busy = true;
        submit.setEnabled(false);
        io(() -> {
            JSONObject body = new JSONObject();
            body.put("firstName", firstName.getText().toString().trim());
            body.put("lastName", lastName.getText().toString().trim());
            body.put("accountType", type);
            body.put("userNumber", number);
            if (!birth.isEmpty()) body.put("dateOfBirth", birth);
            String chosenGender = String.valueOf(gender.getSelectedItem());
            if (!chosenGender.isEmpty()) body.put("gender", chosenGender);
            body.put("hasDisability", disability.isChecked());
            if (disability.isChecked()) body.put("disabilityDetails", disabilityDetails.getText().toString().trim());
            body.put("phoneNumber", phone.getText().toString().trim());
            body.put("email", email.getText().toString().trim());
            body.put("password", pass);
            body.put("confirmPassword", confirm.getText().toString());
            LoginResponse user = LoginResponse.fromJson(ApiClient.post("api/Auth/register", body));
            session.saveSession(user.token, user.fullName, user.email, user.userNumber,
                    android.text.TextUtils.join(",", user.roles), user.expiresAt, user.userId);
            runOnUiThread(() -> {
                new android.app.AlertDialog.Builder(this)
                        .setTitle("Account created")
                        .setMessage("Welcome to Campus Estate Living, " + user.fullName + ".")
                        .setCancelable(false)
                        .setPositiveButton("Continue", (dialog, which) -> {
                Intent intent = new Intent(this, MainActivity.class);
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
                startActivity(intent);
                finish();
                        })
                        .show();
            });
        });
    }

    private boolean strong(String pass) {
        if (pass.length() < 8) return false;
        boolean upper = false, lower = false, digit = false, symbol = false;
        for (int i = 0; i < pass.length(); i++) {
            char ch = pass.charAt(i);
            if (Character.isUpperCase(ch)) upper = true;
            else if (Character.isLowerCase(ch)) lower = true;
            else if (Character.isDigit(ch)) digit = true;
            else symbol = true;
        }
        return upper && lower && digit && symbol;
    }

    private boolean validBirth(String value) {
        if (!value.matches("\\d{4}-\\d{2}-\\d{2}")) return false;
        try {
            int year = Integer.parseInt(value.substring(0, 4));
            int month = Integer.parseInt(value.substring(5, 7));
            int day = Integer.parseInt(value.substring(8, 10));
            Calendar calendar = Calendar.getInstance();
            int now = calendar.get(Calendar.YEAR);
            if (year < now - 120 || month < 1 || month > 12 || day < 1 || day > 31) return false;
            calendar.set(Calendar.HOUR_OF_DAY, 0);
            Calendar born = Calendar.getInstance();
            born.set(year, month - 1, day, 0, 0, 0);
            return !born.after(Calendar.getInstance());
        } catch (Exception ignored) {
            return false;
        }
    }

    @Override
    protected void onError(Exception error) {
        busy = false;
        if (submit != null) submit.setEnabled(true);
        super.onError(error);
    }
}
