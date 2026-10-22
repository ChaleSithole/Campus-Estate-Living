package za.co.campusestateliving.mobile;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.Intent;
import android.content.res.Configuration;
import android.app.Activity;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Build;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.HorizontalScrollView;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

/**
 * Shared luxury design system for Campus Estate Living.
 * Midnight sapphire navy, metallic champagne gold, obsidian glass, and crisp hierarchy.
 */
public final class Ui {
    private Ui() {}

    /** Campus Estate Living branded confirmation used for consequential mobile actions. */
    public static AlertDialog.Builder confirmation(Context context, String title, String message) {
        TextView copy = body(context, message);
        copy.setPadding(dp(context, 18), dp(context, 14), dp(context, 18), dp(context, 14));
        LinearLayout panel = new LinearLayout(context);
        panel.setOrientation(LinearLayout.VERTICAL);
        panel.setBackground(surface(context, 18));
        TextView brand = text(context, "CAMPUS ESTATE LIVING", 11, GOLD_DIM);
        brand.setTypeface(sansBold());
        brand.setLetterSpacing(.12f);
        brand.setPadding(dp(context, 18), dp(context, 16), dp(context, 18), 0);
        panel.addView(brand);
        panel.addView(copy);
        return new AlertDialog.Builder(context).setTitle(title).setView(panel);
    }

    // Core Brand Palette
    public static final int NAVY = 0xFF0A1E34;          // Deep Sapphire Navy
    public static final int NAVY_DEEP = 0xFF051321;     // Obsidian Midnight Navy
    public static final int NAVY_MID = 0xFF0E2339;      // Dark card surface
    public static final int NAVY_SOFT = 0xFF173B5E;     // Gradient highlight
    public static final int NAVY_BORDER = 0xFF1E3A5A;   // Subtle card border
    public static final int GOLD = 0xFFD4AF37;          // Royal Metallic Gold
    public static final int GOLD_LIGHT = 0xFFF3E5AB;    // Champagne Glow
    public static final int GOLD_DIM = 0xFFB89344;      // Muted Gold
    public static final int CREAM = 0xFFF8FAFC;         // Alabaster / Crisp White
    public static final int PAPER = 0xFFFFFFFF;         // Pure White for light cards
    public static final int INK = 0xFF0A1828;           // Deep Charcoal ink
    public static final int MUTED = 0xFF64748B;         // Cool slate muted
    public static final int OK = 0xFF10B981;            // Emerald Green
    public static final int WARN = 0xFFF59E0B;          // Amber Gold
    public static final int DANGER = 0xFFEF4444;        // Crimson Ruby
    public static final int NAV_TEXT = 0xFF8E9BAA;      // Inactive tab slate

    /** Decimal dp, e.g. 1.5f for hairline borders. */
    public static int dp(Context c, float value) {
        return (int) (value * c.getResources().getDisplayMetrics().density + 0.5f);
    }

    public static int dp(Context c, int value) {
        return (int) (value * c.getResources().getDisplayMetrics().density + 0.5f);
    }

    public static boolean night(Context c) {
        String mode = new SessionManager(c).getTheme();
        if ("dark".equals(mode)) return true;
        if ("light".equals(mode)) return false;
        int flags = c.getResources().getConfiguration().uiMode & Configuration.UI_MODE_NIGHT_MASK;
        return flags == Configuration.UI_MODE_NIGHT_YES;
    }

    public static int bg(Context c) { return night(c) ? 0xFF061524 : 0xFFF4F6F9; }
    public static int cardColor(Context c) { return night(c) ? NAVY_MID : PAPER; }
    public static int ink(Context c) { return night(c) ? 0xFFF8FAFC : INK; }
    public static int muted(Context c) { return night(c) ? 0xFF94A3B8 : MUTED; }
    public static int green(Context c) { return night(c) ? GOLD_LIGHT : NAVY; }
    public static int mint(Context c) { return night(c) ? GOLD : NAVY; }
    public static int onAccent(Context c) { return night(c) ? NAVY_DEEP : 0xFFFFFFFF; }
    public static int gold(Context c) { return night(c) ? GOLD_LIGHT : GOLD_DIM; }
    public static int line(Context c) { return night(c) ? 0x2AD4AF37 : 0xFFE2E8F0; }

    /** Headings use bold sans (Capitec-style). Name kept so callers do not change. */
    public static Typeface serif() { return Typeface.create(Typeface.SANS_SERIF, Typeface.BOLD); }
    public static Typeface sansBold() { return Typeface.create(Typeface.SANS_SERIF, Typeface.BOLD); }
    public static Typeface sansMedium() { return Typeface.create(Typeface.SANS_SERIF, Typeface.NORMAL); }

    /** Brand mark (the CE crest tile) at a fixed size. */
    public static ImageView brandMark(Context c, int sizeDp) {
        ImageView mark = new ImageView(c);
        mark.setImageResource(R.drawable.brand_mark);
        mark.setScaleType(ImageView.ScaleType.FIT_CENTER);
        mark.setLayoutParams(new LinearLayout.LayoutParams(dp(c, sizeDp), dp(c, sizeDp)));
        return mark;
    }

    /** Brand mark centred in a vertical column, enclosed in a soft ambient glow. */
    public static View brandCentered(Context c, int sizeDp) {
        FrameLayout container = new FrameLayout(c);
        int outerSize = dp(c, sizeDp + 28);
        container.setLayoutParams(new LinearLayout.LayoutParams(outerSize, outerSize));
        ((LinearLayout.LayoutParams) container.getLayoutParams()).gravity = Gravity.CENTER_HORIZONTAL;
        ((LinearLayout.LayoutParams) container.getLayoutParams()).topMargin = dp(c, 16);
        ((LinearLayout.LayoutParams) container.getLayoutParams()).bottomMargin = dp(c, 16);

        // Ambient radial glow
        View glow = new View(c);
        GradientDrawable glowShape = new GradientDrawable();
        glowShape.setGradientType(GradientDrawable.RADIAL_GRADIENT);
        glowShape.setGradientRadius(dp(c, sizeDp));
        glowShape.setColors(new int[]{0x44D4AF37, 0x00051321});
        glow.setBackground(glowShape);
        container.addView(glow, new FrameLayout.LayoutParams(-1, -1));

        ImageView mark = brandMark(c, sizeDp);
        FrameLayout.LayoutParams markParams = new FrameLayout.LayoutParams(dp(c, sizeDp), dp(c, sizeDp), Gravity.CENTER);
        container.addView(mark, markParams);
        return container;
    }

    public static LinearLayout screen(Activity activity, String tab) {
        LinearLayout outer = new LinearLayout(activity);
        outer.setOrientation(LinearLayout.VERTICAL);
        outer.setBackgroundColor(bg(activity));
        boolean bare = activity instanceof LoginActivity || activity instanceof RegisterActivity;
        if (!bare) outer.addView(header(activity));

        ScrollView scroll = new ScrollView(activity);
        scroll.setFillViewport(true);
        scroll.setVerticalScrollBarEnabled(false);
        LinearLayout root = new LinearLayout(activity);
        root.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(activity, 16);
        root.setPadding(pad, bare ? dp(activity, 28) : dp(activity, 8), pad, dp(activity, 24));
        scroll.addView(root, new LinearLayout.LayoutParams(-1, -2));
        outer.addView(scroll, new LinearLayout.LayoutParams(-1, 0, 1f));
        if (tab != null) outer.addView(nav(activity, tab));
        activity.setContentView(outer);
        return root;
    }

    /** Quiet top bar: profile, search, alerts. The colour lives on the account card, not the chrome. */
    private static View header(Activity activity) {
        LinearLayout bar = new LinearLayout(activity);
        bar.setOrientation(LinearLayout.HORIZONTAL);
        bar.setGravity(Gravity.CENTER_VERTICAL);
        bar.setBackgroundColor(night(activity) ? NAVY_DEEP : CREAM);
        int padH = dp(activity, 16);
        bar.setPadding(padH, dp(activity, 10), padH, dp(activity, 8));

        SessionManager session = new SessionManager(activity);
        FrameLayout avatar = new FrameLayout(activity);
        GradientDrawable avBg = new GradientDrawable();
        avBg.setShape(GradientDrawable.OVAL);
        avBg.setColor(night(activity) ? NAVY_SOFT : NAVY);
        avatar.setBackground(avBg);
        String initials = session.isLoggedIn() ? getInitials(session.getFullName()) : "CE";
        TextView avText = text(activity, initials, 12, CREAM);
        avText.setTypeface(sansBold());
        avatar.addView(avText, new FrameLayout.LayoutParams(-2, -2, Gravity.CENTER));
        int avSize = dp(activity, 40);
        avatar.setOnClickListener(v -> {
            if (session.isLoggedIn() && activity.getClass() != ProfileActivity.class) {
                activity.startActivity(new Intent(activity, ProfileActivity.class));
            }
        });
        bar.addView(avatar, new LinearLayout.LayoutParams(avSize, avSize));

        TextView search = text(activity, "Search", 14, muted(activity));
        GradientDrawable pill = new GradientDrawable();
        pill.setCornerRadius(dp(activity, 22));
        pill.setColor(night(activity) ? NAVY_MID : PAPER);
        search.setBackground(pill);
        search.setGravity(Gravity.CENTER_VERTICAL);
        search.setPadding(dp(activity, 16), 0, dp(activity, 16), 0);
        LinearLayout.LayoutParams searchParams = new LinearLayout.LayoutParams(0, dp(activity, 40), 1f);
        searchParams.leftMargin = dp(activity, 10);
        searchParams.rightMargin = dp(activity, 10);
        search.setOnClickListener(v -> {
            if (!session.isLoggedIn()) return;
            Class<?> dest;
            int category = session.uiCategory();
            if (category == Roles.UI_FINANCE) dest = FinanceActivity.class;
            else if (category == Roles.UI_ADMIN) dest = UsersActivity.class;
            else dest = PaymentsActivity.class;
            if (activity.getClass() != dest) activity.startActivity(new Intent(activity, dest));
        });
        bar.addView(search, searchParams);

        FrameLayout notificationButton = new FrameLayout(activity);
        notificationButton.setTag("notification_button");
        ImageView bell = new ImageView(activity);
        bell.setImageResource(R.drawable.ic_bell);
        bell.setColorFilter(night(activity) ? GOLD_LIGHT : NAVY);
        int bellPad = dp(activity, 8);
        bell.setPadding(bellPad, bellPad, bellPad, bellPad);
        notificationButton.addView(bell, new FrameLayout.LayoutParams(-1, -1));
        TextView badge = text(activity, "", 9, PAPER);
        badge.setTypeface(sansBold());
        badge.setGravity(Gravity.CENTER);
        GradientDrawable badgeBg = new GradientDrawable();
        badgeBg.setShape(GradientDrawable.OVAL);
        badgeBg.setColor(DANGER);
        badge.setBackground(badgeBg);
        int badgeSize = dp(activity, 18);
        FrameLayout.LayoutParams badgeParams = new FrameLayout.LayoutParams(badgeSize, badgeSize, Gravity.TOP | Gravity.END);
        notificationButton.addView(badge, badgeParams);
        notificationButton.setOnClickListener(v -> {
            if (session.isLoggedIn() && activity.getClass() != NotificationsActivity.class) {
                activity.startActivity(new Intent(activity, NotificationsActivity.class));
            }
        });
        notificationButton.setContentDescription("Notifications");
        bar.addView(notificationButton, new LinearLayout.LayoutParams(dp(activity, 40), dp(activity, 40)));
        return bar;
    }

    public static void updateNotificationCount(Activity activity, int count) {
        View content = activity.findViewById(android.R.id.content);
        if (content == null) return;
        FrameLayout button = content.findViewWithTag("notification_button");
        if (button == null) return;
        if (button.getChildCount() < 2 || !(button.getChildAt(1) instanceof TextView)) return;
        TextView badge = (TextView) button.getChildAt(1);
        badge.setVisibility(count > 0 ? View.VISIBLE : View.GONE);
        badge.setText(count > 99 ? "99+" : String.valueOf(count));
        button.setContentDescription(count > 0 ? "Notifications, " + count + " unread" : "Notifications");
    }

    public static TextView text(Context c, String value, int size, int color) {
        TextView view = new TextView(c);
        view.setText(value);
        view.setTextSize(size);
        view.setTextColor(color);
        return view;
    }

    public static TextView eyebrow(Context c, String value) {
        TextView view = text(c, value, 13, muted(c));
        view.setTypeface(sansBold());
        view.setPadding(0, dp(c, 8), 0, dp(c, 4));
        return view;
    }

    public static TextView title(Context c, String value) {
        TextView view = text(c, value, 26, ink(c));
        view.setTypeface(sansBold());
        view.setPadding(0, dp(c, 2), 0, dp(c, 6));
        return view;
    }

    public static TextView body(Context c, String value) {
        TextView view = text(c, value, 14, muted(c));
        view.setPadding(0, dp(c, 2), 0, dp(c, 12));
        view.setLineSpacing(dp(c, 3), 1.05f);
        return view;
    }

    /** Executive gradient hero card with user initials avatar, role badge, and gold outline. */
    public static LinearLayout hero(Context c, String eyebrowText, String name, String subtitle) {
        LinearLayout hero = new LinearLayout(c);
        hero.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(c, 22);
        hero.setPadding(pad, pad, pad, pad);

        GradientDrawable shape = new GradientDrawable(GradientDrawable.Orientation.TL_BR,
                new int[]{0xFF0F3254, 0xFF081C30, 0xFF04101D});
        shape.setCornerRadius(dp(c, 22));
        shape.setStroke(dp(c, 1), 0x55D4AF37);
        hero.setBackground(shape);

        // Top Row: Eyebrow pill on left, Monogram Avatar on right
        LinearLayout topRow = new LinearLayout(c);
        topRow.setOrientation(LinearLayout.HORIZONTAL);
        topRow.setGravity(Gravity.CENTER_VERTICAL);

        LinearLayout eyebrowPill = new LinearLayout(c);
        eyebrowPill.setOrientation(LinearLayout.HORIZONTAL);
        eyebrowPill.setPadding(dp(c, 10), dp(c, 4), dp(c, 10), dp(c, 4));
        GradientDrawable pillShape = new GradientDrawable();
        pillShape.setColor(0x28D4AF37);
        pillShape.setCornerRadius(dp(c, 12));
        pillShape.setStroke(dp(c, 1), 0x55D4AF37);
        eyebrowPill.setBackground(pillShape);

        TextView eyebrowView = text(c, eyebrowText.toUpperCase(), 10, GOLD_LIGHT);
        eyebrowView.setLetterSpacing(0.14f);
        eyebrowView.setTypeface(sansBold());
        eyebrowPill.addView(eyebrowView);
        topRow.addView(eyebrowPill, new LinearLayout.LayoutParams(-2, -2, 0f));

        View spacer = new View(c);
        topRow.addView(spacer, new LinearLayout.LayoutParams(0, 1, 1f));

        // Monogram
        String initials = getInitials(name);
        FrameLayout av = new FrameLayout(c);
        GradientDrawable avShape = new GradientDrawable();
        avShape.setShape(GradientDrawable.OVAL);
        avShape.setColor(0x33D4AF37);
        avShape.setStroke(dp(c, 1), GOLD);
        av.setBackground(avShape);
        TextView avTv = text(c, initials, 12, GOLD_LIGHT);
        avTv.setTypeface(sansBold());
        av.addView(avTv, new FrameLayout.LayoutParams(-2, -2, Gravity.CENTER));
        topRow.addView(av, new LinearLayout.LayoutParams(dp(c, 34), dp(c, 34)));

        hero.addView(topRow);

        // Main User Name
        TextView heading = text(c, name, 24, 0xFFFFFFFF);
        heading.setTypeface(serif());
        heading.setPadding(0, dp(c, 12), 0, dp(c, 4));
        hero.addView(heading);

        // Subtitle / Role Tag
        if (subtitle != null && !subtitle.isEmpty()) {
            TextView sub = text(c, subtitle, 13, 0xFFCCD8E6);
            sub.setPadding(0, 0, 0, 0);
            hero.addView(sub);
        }

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = dp(c, 16);
        hero.setLayoutParams(params);
        return hero;
    }

    public static LinearLayout card(Context c) {
        LinearLayout card = new LinearLayout(c);
        card.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(c, 18);
        card.setPadding(pad, pad, pad, pad);
        card.setBackground(surface(c, 20));
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP && !night(c)) {
            card.setElevation(dp(c, 3));
        }
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = dp(c, 14);
        card.setLayoutParams(params);
        return card;
    }

    /** Metric tile with uppercase tracking label and bold figure. */
    public static LinearLayout statTile(Context c, String label, String value) {
        LinearLayout tile = new LinearLayout(c);
        tile.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(c, 16);
        tile.setPadding(pad, pad, pad, pad);

        GradientDrawable shape = new GradientDrawable();
        shape.setColor(cardColor(c));
        shape.setCornerRadius(dp(c, 18));
        shape.setStroke(dp(c, 1), line(c));
        tile.setBackground(shape);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP && !night(c)) {
            tile.setElevation(dp(c, 2));
        }

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, -2, 1f);
        params.setMargins(dp(c, 4), 0, dp(c, 4), dp(c, 14));
        tile.setLayoutParams(params);

        TextView caption = text(c, label.toUpperCase(), 10, muted(c));
        caption.setLetterSpacing(0.08f);
        caption.setTypeface(sansBold());

        TextView figure = text(c, value, 21, green(c));
        figure.setTypeface(serif());
        figure.setPadding(0, dp(c, 8), 0, 0);

        tile.addView(caption);
        tile.addView(figure);
        return tile;
    }

    private static GradientDrawable surface(Context c, int radiusDp) {
        GradientDrawable shape = new GradientDrawable();
        shape.setColor(cardColor(c));
        shape.setCornerRadius(dp(c, radiusDp));
        shape.setStroke(dp(c, 1), line(c));
        return shape;
    }

    public static void pair(LinearLayout card, String label, String value) {
        Context c = card.getContext();
        TextView view = text(c, label.toUpperCase(), 11, muted(c));
        view.setLetterSpacing(0.08f);
        view.setTypeface(sansBold());
        view.setPadding(0, dp(c, 10), 0, dp(c, 2));
        card.addView(view);

        TextView amount = text(c, value, 15, ink(c));
        amount.setTypeface(sansMedium());
        card.addView(amount);
    }

    /** Self-contained pill badge with indicator dot and glowing outline. */
    public static TextView status(Context c, String name) {
        int color = muted(c);
        String dot = "● ";
        if ("Pending".equalsIgnoreCase(name) || "Open".equalsIgnoreCase(name)) {
            color = WARN;
        } else if ("Confirmed".equalsIgnoreCase(name) || "Active".equalsIgnoreCase(name)) {
            color = OK;
        } else if ("Failed".equalsIgnoreCase(name) || "Cancelled".equalsIgnoreCase(name) || "Inactive".equalsIgnoreCase(name)) {
            color = DANGER;
        }
        TextView view = text(c, dot + (name == null || name.isEmpty() ? "UNKNOWN" : name.toUpperCase()), 11, color);
        view.setTypeface(sansBold());
        view.setLetterSpacing(0.06f);

        GradientDrawable pill = new GradientDrawable();
        int alphaFill = (color & 0x00FFFFFF) | 0x24000000;
        pill.setColor(alphaFill);
        pill.setCornerRadius(dp(c, 14));
        pill.setStroke(dp(c, 1), (color & 0x00FFFFFF) | 0x66000000);
        view.setBackground(pill);

        int padH = dp(c, 12);
        int padV = dp(c, 5);
        view.setPadding(padH, padV, padH, padV);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-2, -2);
        params.topMargin = dp(c, 8);
        params.bottomMargin = dp(c, 4);
        view.setLayoutParams(params);
        return view;
    }

    public static EditText field(Context c, String hint, int inputType) {
        EditText field = new EditText(c);
        field.setHint(hint);
        field.setSingleLine(true);
        field.setInputType(inputType);
        field.setTextColor(ink(c));
        field.setHintTextColor(muted(c));
        field.setTextSize(15);
        field.setMinHeight(dp(c, 52));

        GradientDrawable shape = new GradientDrawable();
        shape.setColor(night(c) ? 0xFF091A2B : 0xFFFFFFFF);
        shape.setCornerRadius(dp(c, 14));
        shape.setStroke(dp(c, 1), night(c) ? 0xFF223F5E : 0xFFCBD5E1);
        field.setBackground(shape);

        int padH = dp(c, 16);
        int padV = dp(c, 14);
        field.setPadding(padH, padV, padH, padV);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = dp(c, 14);
        field.setLayoutParams(params);
        return field;
    }

    /** Primary = Metallic Gold gradient with obsidian ink. Secondary = Outline with gold stroke. */
    public static Button button(Context c, String label, boolean primary) {
        Button button = new Button(c);
        button.setText(label);
        button.setAllCaps(false);
        button.setTextSize(15);
        button.setTypeface(sansBold());
        button.setMinHeight(dp(c, 52));
        button.setElevation(dp(c, 2));
        button.setStateListAnimator(null);

        GradientDrawable shape = new GradientDrawable();
        shape.setCornerRadius(dp(c, 26));
        if (primary) {
            shape.setColor(night(c) ? GOLD : NAVY);
            button.setTextColor(night(c) ? NAVY_DEEP : PAPER);
            button.setBackground(shape);
        } else {
            shape.setColor(cardColor(c));
            shape.setStroke(dp(c, 1), gold(c));
            button.setTextColor(night(c) ? GOLD_LIGHT : NAVY);
            button.setBackground(shape);
        }

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = dp(c, 12);
        button.setLayoutParams(params);
        return button;
    }

    public static LinearLayout tappable(Context c, String heading, String detail, Runnable action) {
        LinearLayout card = new LinearLayout(c);
        card.setOrientation(LinearLayout.VERTICAL);
        int rowPad = dp(c, 16);
        card.setPadding(rowPad, rowPad, rowPad, rowPad);
        GradientDrawable flat = new GradientDrawable();
        flat.setColor(cardColor(c));
        flat.setCornerRadius(dp(c, 18));
        card.setBackground(flat);
        LinearLayout.LayoutParams flatParams = new LinearLayout.LayoutParams(-1, -2);
        flatParams.bottomMargin = dp(c, 8);
        card.setLayoutParams(flatParams);
        LinearLayout row = new LinearLayout(c);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER_VERTICAL);

        LinearLayout labels = new LinearLayout(c);
        labels.setOrientation(LinearLayout.VERTICAL);
        TextView title = text(c, heading, 16, green(c));
        title.setTypeface(serif());
        labels.addView(title);
        if (detail != null && !detail.isEmpty()) {
            TextView body = text(c, detail, 13, muted(c));
            body.setPadding(0, dp(c, 4), 0, 0);
            body.setLineSpacing(dp(c, 2), 1f);
            labels.addView(body);
        }
        row.addView(labels, new LinearLayout.LayoutParams(0, -2, 1f));

        card.addView(row);
        card.setClickable(true);
        card.setFocusable(true);
        card.setOnClickListener(v -> action.run());
        return card;
    }

    /** Quick action circular icon button for dashboard action grids. */
    public static LinearLayout quickAction(Context c, int icon, String label, Runnable action) {
        LinearLayout btn = new LinearLayout(c);
        btn.setOrientation(LinearLayout.VERTICAL);
        btn.setGravity(Gravity.CENTER);
        int pad = dp(c, 12);
        btn.setPadding(pad, dp(c, 14), pad, dp(c, 14));

        GradientDrawable bg = new GradientDrawable();
        bg.setColor(0x00000000);
        bg.setCornerRadius(dp(c, 16));
        btn.setBackground(bg);

        FrameLayout iconFrame = new FrameLayout(c);
        GradientDrawable iconBg = new GradientDrawable();
        iconBg.setShape(GradientDrawable.OVAL);
        iconBg.setColor(night(c) ? 0x33D4AF37 : 0x1AD4AF37);
        iconFrame.setBackground(iconBg);
        int iconSize = dp(c, 58);

        ImageView iconView = new ImageView(c);
        iconView.setImageResource(icon);
        iconView.setColorFilter(night(c) ? GOLD_LIGHT : NAVY);
        int glyphSize = dp(c, 27);
        iconFrame.addView(iconView, new FrameLayout.LayoutParams(glyphSize, glyphSize, Gravity.CENTER));
        btn.addView(iconFrame, new LinearLayout.LayoutParams(iconSize, iconSize));

        TextView tv = text(c, label, 11, ink(c));
        tv.setTypeface(sansBold());
        tv.setGravity(Gravity.CENTER);
        tv.setPadding(0, dp(c, 8), 0, 0);
        btn.addView(tv);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, -2, 1f);
        params.setMargins(dp(c, 4), 0, dp(c, 4), dp(c, 10));
        btn.setLayoutParams(params);

        btn.setClickable(true);
        btn.setFocusable(true);
        btn.setOnClickListener(v -> action.run());
        return btn;
    }

    /** Realistic luxury credit card widget. */
    public static LinearLayout creditCardWidget(Context c, String brand, String last4, String holder, int expMonth, int expYear) {
        LinearLayout card = new LinearLayout(c);
        card.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(c, 20);
        card.setPadding(pad, pad, pad, pad);

        GradientDrawable shape = new GradientDrawable(GradientDrawable.Orientation.TL_BR,
                new int[]{0xFF0E3154, 0xFF081C30, 0xFF030C16});
        shape.setCornerRadius(dp(c, 20));
        shape.setStroke(dp(c, 1), 0x66D4AF37);
        card.setBackground(shape);

        // Top row: Chip and Brand
        LinearLayout top = new LinearLayout(c);
        top.setOrientation(LinearLayout.HORIZONTAL);
        top.setGravity(Gravity.CENTER_VERTICAL);

        LinearLayout chip = new LinearLayout(c);
        chip.setOrientation(LinearLayout.VERTICAL);
        GradientDrawable chipShape = new GradientDrawable();
        chipShape.setColor(0xFFD4AF37);
        chipShape.setCornerRadius(dp(c, 4));
        chip.setBackground(chipShape);
        top.addView(chip, new LinearLayout.LayoutParams(dp(c, 36), dp(c, 26)));

        View spacer = new View(c);
        top.addView(spacer, new LinearLayout.LayoutParams(0, 1, 1f));

        TextView brandTv = text(c, brand.toUpperCase(), 15, GOLD_LIGHT);
        brandTv.setTypeface(sansBold());
        brandTv.setLetterSpacing(0.12f);
        top.addView(brandTv);
        card.addView(top);

        // Number
        TextView number = text(c, "••••  ••••  ••••  " + last4, 18, 0xFFFFFFFF);
        number.setTypeface(sansBold());
        number.setLetterSpacing(0.12f);
        number.setPadding(0, dp(c, 20), 0, dp(c, 16));
        card.addView(number);

        // Bottom row: Holder & Expiry
        LinearLayout bot = new LinearLayout(c);
        bot.setOrientation(LinearLayout.HORIZONTAL);
        bot.setGravity(Gravity.CENTER_VERTICAL);

        LinearLayout hCol = new LinearLayout(c);
        hCol.setOrientation(LinearLayout.VERTICAL);
        TextView hLabel = text(c, "CARDHOLDER", 9, 0xFF94A3B8);
        hLabel.setLetterSpacing(0.08f);
        TextView hName = text(c, holder.toUpperCase(), 13, 0xFFF8FAFC);
        hName.setTypeface(sansBold());
        hCol.addView(hLabel);
        hCol.addView(hName);
        bot.addView(hCol, new LinearLayout.LayoutParams(0, -2, 1f));

        LinearLayout eCol = new LinearLayout(c);
        eCol.setOrientation(LinearLayout.VERTICAL);
        TextView eLabel = text(c, "EXPIRES", 9, 0xFF94A3B8);
        eLabel.setLetterSpacing(0.08f);
        String exp = String.format("%02d/%02d", expMonth, expYear % 100);
        TextView eDate = text(c, exp, 13, 0xFFF8FAFC);
        eDate.setTypeface(sansBold());
        eCol.addView(eLabel);
        eCol.addView(eDate);
        bot.addView(eCol);

        card.addView(bot);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, -2);
        params.bottomMargin = dp(c, 14);
        card.setLayoutParams(params);
        return card;
    }

    /** Extracts two-letter initials from a full name. */
    public static String getInitials(String name) {
        if (name == null || name.trim().isEmpty()) return "CE";
        String[] parts = name.trim().split("\\s+");
        if (parts.length == 1) return parts[0].substring(0, Math.min(2, parts[0].length())).toUpperCase();
        return ("" + parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
    }

    /** Role-specific bottom bar. Profile stays available from the avatar on every role. */
    private static View nav(Activity activity, String selected) {
        HorizontalScrollView scroller = new HorizontalScrollView(activity);
        scroller.setHorizontalScrollBarEnabled(false);
        scroller.setFillViewport(true);
        scroller.setBackgroundColor(night(activity) ? NAVY_DEEP : PAPER);

        LinearLayout bar = new LinearLayout(activity);
        bar.setOrientation(LinearLayout.HORIZONTAL);
        bar.setPadding(dp(activity, 4), dp(activity, 6), dp(activity, 4), dp(activity, 8));

        SessionManager session = new SessionManager(activity);
        int category = session.uiCategory();
        if (category == Roles.UI_ADMIN) {
            addTab(bar, activity, "Home", R.drawable.ic_nav_home, MainActivity.class, "Home".equals(selected));
            addTab(bar, activity, "Users", R.drawable.ic_nav_people, UsersActivity.class, "Users".equals(selected));
            addTab(bar, activity, "Buildings", R.drawable.ic_nav_stay, BuildingsActivity.class, "Buildings".equals(selected));
            addTab(bar, activity, "Messages", R.drawable.ic_nav_people, StaffMessagesActivity.class, "Messages".equals(selected));
            addTab(bar, activity, "More", R.drawable.ic_nav_people, MoreActivity.class, "More".equals(selected));
        } else if (category == Roles.UI_FINANCE) {
            addTab(bar, activity, "Home", R.drawable.ic_nav_home, MainActivity.class, "Home".equals(selected));
            addTab(bar, activity, "Transactions", R.drawable.ic_nav_transact, FinanceActivity.class, "Transactions".equals(selected));
            addTab(bar, activity, "Charges", R.drawable.ic_nav_card, ChargesActivity.class, "Charges".equals(selected));
            addTab(bar, activity, "Messages", R.drawable.ic_nav_people, StaffMessagesActivity.class, "Messages".equals(selected));
            addTab(bar, activity, "More", R.drawable.ic_nav_explore, MoreActivity.class, "More".equals(selected));
        } else {
            addTab(bar, activity, "Home", R.drawable.ic_nav_home, MainActivity.class, "Home".equals(selected));
            addTab(bar, activity, "Payments", R.drawable.ic_nav_transact, PaymentsActivity.class, "Payments".equals(selected));
            addTab(bar, activity, "History", R.drawable.ic_nav_card, HistoryActivity.class, "History".equals(selected));
            addTab(bar, activity, "Stay", R.drawable.ic_nav_stay, AccommodationActivity.class, "Stay".equals(selected));
            addTab(bar, activity, "Profile", R.drawable.ic_nav_people, ProfileActivity.class, "Profile".equals(selected));
        }
        scroller.addView(bar, new FrameLayout.LayoutParams(-1, -2));
        return scroller;
    }

    private static void addTab(LinearLayout bar, Activity activity, String label, int iconRes, Class<?> destination, boolean selected) {
        LinearLayout tab = new LinearLayout(activity);
        tab.setOrientation(LinearLayout.VERTICAL);
        tab.setGravity(Gravity.CENTER);
        tab.setPadding(dp(activity, 6), dp(activity, 6), dp(activity, 6), dp(activity, 4));
        tab.setMinimumWidth(dp(activity, 68));

        int color = selected ? (night(activity) ? GOLD_LIGHT : NAVY) : muted(activity);

        ImageView iconView = new ImageView(activity);
        iconView.setImageResource(iconRes);
        iconView.setColorFilter(color);
        int size = dp(activity, 22);
        tab.addView(iconView, new LinearLayout.LayoutParams(size, size));

        TextView labelView = new TextView(activity);
        labelView.setText(label);
        labelView.setTextSize(11);
        labelView.setGravity(Gravity.CENTER);
        labelView.setMaxLines(1);
        labelView.setTypeface(selected ? sansBold() : sansMedium());
        labelView.setTextColor(color);
        labelView.setPadding(0, dp(activity, 3), 0, 0);
        tab.addView(labelView);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, -2, 1f);
        tab.setLayoutParams(params);
        tab.setClickable(true);
        tab.setFocusable(true);
        tab.setOnClickListener(v -> {
            if (activity.getClass() == destination) return;
            Intent intent = new Intent(activity, destination);
            intent.addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT | Intent.FLAG_ACTIVITY_SINGLE_TOP);
            activity.startActivity(intent);
        });
        bar.addView(tab);
    }

    /** One payment row, inflated from res/layout/item_payment.xml. */
    public static android.view.View paymentRow(Context c, android.view.ViewGroup parent, org.json.JSONObject payment, Runnable onClick) {
        android.view.View row = android.view.LayoutInflater.from(c).inflate(R.layout.item_payment, parent, false);
        ((TextView) row.findViewById(R.id.itemCategory)).setText(payment.optString("categoryName", "Tariff"));
        ((TextView) row.findViewById(R.id.itemRef)).setText("Ref: " + payment.optString("referenceNumber") + "  \u00b7  " + payment.optString("paymentMethod", "EFT"));
        ((TextView) row.findViewById(R.id.itemAmount)).setText(Format.money(payment.optDouble("amount", 0)));
        ((android.widget.FrameLayout) row.findViewById(R.id.itemStatusSlot)).addView(status(c, payment.optString("status")));
        row.setOnClickListener(v -> onClick.run());
        return row;
    }

    /** One label/value line, inflated from res/layout/item_info.xml. */
    public static android.view.View infoRow(Context c, android.view.ViewGroup parent, String label, String value) {
        android.view.View row = android.view.LayoutInflater.from(c).inflate(R.layout.item_info, parent, false);
        ((TextView) row.findViewById(R.id.infoLabel)).setText(label);
        ((TextView) row.findViewById(R.id.infoValue)).setText(value);
        return row;
    }
}
