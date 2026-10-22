package za.co.campusestateliving.mobile;

import za.co.campusestateliving.mobile.api.ApiClient;
import android.content.Intent;
import android.text.InputType;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.HorizontalScrollView;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Spinner;

import org.json.JSONArray;
import org.json.JSONObject;

import java.net.URLEncoder;

public class UsersActivity extends CelActivity {
    private String role = "";
    private String query = "";

    @Override
    protected void onResume() {
        super.onResume();
        if (!session.isLoggedIn()) return;
        if (!session.isStaffSide()) {
            toast("People management is limited to administrators and financial officers.");
            finish();
            return;
        }
        if (root == null) bind(session.isFinanceUi() ? "Accounts" : session.isAdministratorUi() ? "Users" : null);
        load();
    }

    private void load() {
        root.removeAllViews();
        root.addView(Ui.body(this, "Loading people..."));
        io(() -> {
            String path = "api/Users?role=" + URLEncoder.encode(role, "UTF-8") + "&q=" + URLEncoder.encode(query, "UTF-8");
            JSONArray users = ApiClient.getArray(path, session.getToken());
            runOnUiThread(() -> show(users));
        });
    }

    private void show(JSONArray users) {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, session.isAdministratorUi() ? "User management" : "Resident accounts"));
        root.addView(Ui.title(this, session.isAdministratorUi() ? "Users and officers" : "Accounts"));
        LinearLayout toolbar = Ui.card(this);
        TextView count = Ui.text(this, users.length() + (users.length() == 1 ? " person" : " people"), 14, Ui.muted(this));
        count.setTypeface(Ui.sansBold());
        toolbar.addView(count);
        EditText search = Ui.field(this, "Search name, email or number", InputType.TYPE_CLASS_TEXT);
        search.setText(query);
        search.setImeOptions(android.view.inputmethod.EditorInfo.IME_ACTION_SEARCH);
        search.setOnEditorActionListener((view, actionId, event) -> {
            query = search.getText().toString().trim();
            load();
            return true;
        });
        toolbar.addView(search);
        Button find = Ui.button(this, "Search accounts", true);
        find.setOnClickListener(v -> {
            query = search.getText().toString().trim();
            load();
        });
        toolbar.addView(find);
        root.addView(toolbar);

        root.addView(Ui.eyebrow(this, "Filter by role"));
        HorizontalScrollView filtersScroll = new HorizontalScrollView(this);
        filtersScroll.setHorizontalScrollBarEnabled(false);
        filtersScroll.setClipToPadding(false);
        LinearLayout.LayoutParams filterScrollParams = new LinearLayout.LayoutParams(-1, -2);
        filterScrollParams.bottomMargin = Ui.dp(this, 10);
        filtersScroll.setLayoutParams(filterScrollParams);
        LinearLayout filters = new LinearLayout(this);
        filters.setOrientation(LinearLayout.HORIZONTAL);
        filtersScroll.addView(filters);
        addRole(filters, "All", "");
        addRole(filters, "Students", Roles.STUDENT);
        addRole(filters, "Staff", Roles.STAFF);
        if (session.isAdministratorUi()) {
            addRole(filters, "Officers", Roles.FINANCIAL_OFFICER);
            addRole(filters, "Administrators", Roles.ADMINISTRATOR);
        }
        root.addView(filtersScroll);
        if (session.hasRole(Roles.ADMINISTRATOR)) {
            Button create = Ui.button(this, "Create account", true);
            LinearLayout.LayoutParams createParams = new LinearLayout.LayoutParams(-1, -2);
            createParams.topMargin = Ui.dp(this, 6);
            createParams.bottomMargin = Ui.dp(this, 18);
            create.setLayoutParams(createParams);
            create.setOnClickListener(v -> showCreate());
            root.addView(create);
        }
        LinearLayout resultsHeader = new LinearLayout(this);
        resultsHeader.setOrientation(LinearLayout.HORIZONTAL);
        resultsHeader.setGravity(android.view.Gravity.CENTER_VERTICAL);
        TextView resultsTitle = Ui.text(this, "Directory", 18, Ui.ink(this));
        resultsTitle.setTypeface(Ui.sansBold());
        resultsHeader.addView(resultsTitle, new LinearLayout.LayoutParams(0, -2, 1f));
        TextView activeFilter = Ui.text(this, role.isEmpty() ? "All roles" : Format.roles(new JSONArray().put(role)), 12, Ui.muted(this));
        resultsHeader.addView(activeFilter);
        root.addView(resultsHeader);
        if (users.length() == 0) {
            LinearLayout empty = Ui.card(this);
            TextView emptyTitle = Ui.text(this, "No accounts found", 17, Ui.ink(this));
            emptyTitle.setTypeface(Ui.sansBold());
            empty.addView(emptyTitle);
            empty.addView(Ui.body(this, query.isEmpty() ? "Try a different role filter or create an account." : "Check the search text or try another role filter."));
            root.addView(empty);
        }
        for (int i = 0; i < users.length(); i++) {
            JSONObject user = users.optJSONObject(i);
            if (user == null) continue;
            int id = user.optInt("userId");
            LinearLayout account = Ui.card(this);
            LinearLayout top = new LinearLayout(this);
            top.setOrientation(LinearLayout.HORIZONTAL);
            top.setGravity(android.view.Gravity.CENTER_VERTICAL);
            TextView fullName = Ui.text(this, user.optString("fullName"), 16, Ui.ink(this));
            fullName.setTypeface(Ui.sansBold());
            top.addView(fullName, new LinearLayout.LayoutParams(0, -2, 1f));
            top.addView(Ui.status(this, user.optBoolean("isActive") ? "Active" : "Inactive"));
            account.addView(top);
            account.addView(Ui.text(this, Format.roles(user.optJSONArray("roles")), 13, Ui.gold(this)));
            Ui.pair(account, "User number", user.optString("userNumber"));
            String email = user.optString("email");
            if (!email.isEmpty() && !"null".equalsIgnoreCase(email)) Ui.pair(account, "Email", email);
            String phone = user.optString("phoneNumber");
            if (!phone.isEmpty() && !"null".equalsIgnoreCase(phone)) Ui.pair(account, "Phone", phone);
            String accountNumber = Format.text(user.optString("accountNumber"));
            if (!accountNumber.isEmpty() && !"—".equals(accountNumber)) Ui.pair(account, "Estate account", accountNumber);
            Button details = Ui.button(this, "View account", false);
            details.setOnClickListener(v -> {
                Intent intent = new Intent(this, UserDetailActivity.class);
                intent.putExtra("userId", id);
                startActivity(intent);
            });
            account.addView(details);
            account.setOnClickListener(v -> {
                Intent intent = new Intent(this, UserDetailActivity.class);
                intent.putExtra("userId", id);
                startActivity(intent);
            });
            root.addView(account);
        }
    }

    private void addRole(LinearLayout filters, String label, String value) {
        Button button = Ui.button(this, label, value.equals(role));
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-2, -2);
        params.rightMargin = Ui.dp(this, 8);
        params.bottomMargin = Ui.dp(this, 4);
        button.setLayoutParams(params);
        button.setMinHeight(Ui.dp(this, 48));
        button.setTextSize(13);
        button.setOnClickListener(v -> {
            role = value;
            load();
        });
        filters.addView(button);
    }

    private void showCreate() {
        root.removeAllViews();
        root.addView(Ui.eyebrow(this, "USER MANAGEMENT"));
        root.addView(Ui.title(this, "Create account"));
        root.addView(Ui.body(this, "Add a person to the estate directory. Complete the required details, then choose the account access they need."));

        LinearLayout identity = formCard("Personal details", "Name and identification");
        LinearLayout nameRow = new LinearLayout(this);
        nameRow.setOrientation(LinearLayout.HORIZONTAL);
        EditText first = formField(identity, "First name *", InputType.TYPE_CLASS_TEXT);
        EditText last = formField(identity, "Last name *", InputType.TYPE_CLASS_TEXT);
        LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(0, -2, 1f);
        nameParams.rightMargin = Ui.dp(this, 6);
        first.setLayoutParams(nameParams);
        LinearLayout.LayoutParams lastParams = new LinearLayout.LayoutParams(0, -2, 1f);
        lastParams.leftMargin = Ui.dp(this, 6);
        last.setLayoutParams(lastParams);
        identity.removeView(first);
        identity.removeView(last);
        nameRow.addView(first);
        nameRow.addView(last);
        identity.addView(nameRow);
        EditText number = formField(identity, "Identification number *", InputType.TYPE_CLASS_NUMBER);

        LinearLayout access = formCard("Account access", "Choose the role and estate account options");
        Spinner roleSpinner = formSpinner(access, "Role", new String[]{Roles.STUDENT, Roles.STAFF, Roles.FINANCIAL_OFFICER, Roles.ADMINISTRATOR});
        CheckBox estate = new CheckBox(this);
        estate.setText("Create an estate account");
        estate.setTextColor(Ui.ink(this));
        estate.setChecked(true);
        estate.setPadding(0, Ui.dp(this, 4), 0, Ui.dp(this, 4));
        access.addView(estate);

        LinearLayout contact = formCard("Contact details", "Used for account communication");
        EditText email = formField(contact, "Email address *", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS);
        EditText phone = formField(contact, "Phone number", InputType.TYPE_CLASS_PHONE);

        LinearLayout profile = formCard("Additional details", "Optional personal information");
        EditText birth = formField(profile, "Date of birth (yyyy-mm-dd)", InputType.TYPE_CLASS_DATETIME | InputType.TYPE_DATETIME_VARIATION_DATE);
        Spinner gender = formSpinner(profile, "Gender (optional)", new String[]{"", "Female", "Male", "Non-binary", "Prefer not to say"});
        CheckBox disability = new CheckBox(this);
        disability.setText("Has a disability");
        disability.setTextColor(Ui.ink(this));
        profile.addView(disability);
        EditText details = formField(profile, "Disability details", InputType.TYPE_CLASS_TEXT);
        details.setVisibility(View.GONE);
        disability.setOnCheckedChangeListener((button, checked) -> details.setVisibility(checked ? View.VISIBLE : View.GONE));

        LinearLayout security = formCard("Sign-in security", "Set a temporary password for the new account");
        EditText password = formField(security, "Temporary password *", InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        password.setTransformationMethod(android.text.method.PasswordTransformationMethod.getInstance());

        root.addView(identity);
        root.addView(access);
        root.addView(contact);
        root.addView(profile);
        root.addView(security);

        LinearLayout actions = new LinearLayout(this);
        actions.setOrientation(LinearLayout.HORIZONTAL);
        Button save = Ui.button(this, "Create", true);
        Button back = Ui.button(this, "Cancel", false);
        LinearLayout.LayoutParams saveParams = new LinearLayout.LayoutParams(0, -2, 1f);
        saveParams.rightMargin = Ui.dp(this, 6);
        save.setLayoutParams(saveParams);
        LinearLayout.LayoutParams backParams = new LinearLayout.LayoutParams(0, -2, 1f);
        backParams.leftMargin = Ui.dp(this, 6);
        back.setLayoutParams(backParams);
        save.setOnClickListener(v -> {
            String given = first.getText().toString().trim();
            String family = last.getText().toString().trim();
            String chosenRole = String.valueOf(roleSpinner.getSelectedItem());
            String userNumber = number.getText().toString().trim();
            String address = email.getText().toString().trim();
            String phoneNumber = phone.getText().toString().trim();
            String dob = birth.getText().toString().trim();
            String chosenGender = String.valueOf(gender.getSelectedItem());
            boolean hasDisability = disability.isChecked();
            String disabilityText = details.getText().toString().trim();
            String temporaryPassword = password.getText().toString();
            boolean createAccount = estate.isChecked();
            io(() -> {
                JSONObject body = new JSONObject();
                body.put("firstName", given);
                body.put("lastName", family);
                body.put("role", chosenRole);
                body.put("userNumber", userNumber);
                if (!dob.isEmpty()) body.put("dateOfBirth", dob);
                if (!chosenGender.isEmpty()) body.put("gender", chosenGender);
                body.put("hasDisability", hasDisability);
                if (hasDisability) body.put("disabilityDetails", disabilityText);
                body.put("phoneNumber", phoneNumber);
                body.put("email", address);
                body.put("password", temporaryPassword);
                body.put("createEstateAccount", createAccount);
                ApiClient.post("api/Users", body, session.getToken());
                runOnUiThread(() -> {
                    toast("Account created.");
                    load();
                });
            });
        });
        back.setOnClickListener(v -> load());
        actions.addView(save);
        actions.addView(back);
        root.addView(actions);
    }

    private LinearLayout formCard(String title, String subtitle) {
        LinearLayout card = Ui.card(this);
        TextView heading = Ui.text(this, title, 17, Ui.ink(this));
        heading.setTypeface(Ui.sansBold());
        card.addView(heading);
        TextView description = Ui.text(this, subtitle, 12, Ui.muted(this));
        description.setPadding(0, Ui.dp(this, 3), 0, Ui.dp(this, 10));
        card.addView(description);
        return card;
    }

    private EditText formField(LinearLayout parent, String hint, int inputType) {
        EditText input = Ui.field(this, hint, inputType);
        parent.addView(input);
        return input;
    }

    private Spinner formSpinner(LinearLayout parent, String label, String[] values) {
        TextView caption = Ui.text(this, label, 12, Ui.muted(this));
        caption.setTypeface(Ui.sansBold());
        caption.setPadding(0, Ui.dp(this, 4), 0, Ui.dp(this, 4));
        parent.addView(caption);
        Spinner spinner = new Spinner(this);
        spinner.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, values));
        spinner.setMinimumHeight(Ui.dp(this, 48));
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = Ui.dp(this, 10);
        spinner.setLayoutParams(params);
        parent.addView(spinner);
        return spinner;
    }
}
