package za.co.campusestateliving.mobile;

import android.animation.ValueAnimator;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.PathMeasure;
import android.graphics.RadialGradient;
import android.graphics.Shader;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.provider.Settings;
import android.view.Gravity;
import android.view.View;
import android.view.animation.AccelerateInterpolator;
import android.view.animation.DecelerateInterpolator;
import android.view.animation.LinearInterpolator;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.List;

/**
 * Launch screen. Blue and gold light trails sweep toward the official crest,
 * followed by a quick wordmark reveal. Tapping anywhere skips ahead.
 */
public class SplashActivity extends Activity {
    private static final long MIN_HOLD_MS = 5500;
    private static final long REVEAL_MS = 1800;
    private static final long LOGO_HOLD_MS = 2000;
    // Let the emblem travel forward with a cinematic, eased push instead of a snap zoom.
    private static final long RUSH_MS = 1250;

    private final Handler handler = new Handler(Looper.getMainLooper());
    private boolean routed;
    private View logoStack;
    private ValueAnimator trailFlowAnimator;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(0xFF071426);
        getWindow().setNavigationBarColor(0xFF071426);

        FrameLayout stage = new FrameLayout(this);
        stage.setBackground(new GradientDrawable(GradientDrawable.Orientation.TOP_BOTTOM,
                new int[]{0xFF0B1D38, 0xFF071426, 0xFF0B1D38}));
        stage.setOnClickListener(v -> route());
        setContentView(stage);

        LightTrailsView trails = new LightTrailsView(this);
        stage.addView(trails, new FrameLayout.LayoutParams(-1, -1));

        LinearLayout column = new LinearLayout(this);
        column.setOrientation(LinearLayout.VERTICAL);
        column.setGravity(Gravity.CENTER);
        stage.addView(column, new FrameLayout.LayoutParams(-1, -1, Gravity.CENTER));

        // Crest stack: the existing official emblem rests above the converging trails.
        FrameLayout markStack = new FrameLayout(this);
        logoStack = markStack;
        View glow = new View(this);
        GradientDrawable glowShape = new GradientDrawable();
        glowShape.setGradientType(GradientDrawable.RADIAL_GRADIENT);
        glowShape.setGradientRadius(Ui.dp(this, 150));
        glowShape.setColors(new int[]{0x66D5AD55, 0x001769AA, 0x000B1D38});
        glow.setBackground(glowShape);
        glow.setAlpha(0f);
        glow.setScaleX(0.6f);
        glow.setScaleY(0.6f);
        markStack.addView(glow, new FrameLayout.LayoutParams(Ui.dp(this, 300), Ui.dp(this, 300), Gravity.CENTER));

        ImageView mark = new ImageView(this);
        mark.setImageResource(R.drawable.brand_mark_splash);
        mark.setScaleType(ImageView.ScaleType.FIT_CENTER);
        mark.setAlpha(0f);
        mark.setScaleX(0.3f);
        mark.setScaleY(0.3f);
        markStack.addView(mark, new FrameLayout.LayoutParams(Ui.dp(this, 116), Ui.dp(this, 116), Gravity.CENTER));

        column.addView(markStack, new LinearLayout.LayoutParams(Ui.dp(this, 300), Ui.dp(this, 220)));

        // Wordmark, one letter per view so each can rise in on its own.
        List<TextView> letters = new ArrayList<>();
        LinearLayout wordmark = new LinearLayout(this);
        wordmark.setOrientation(LinearLayout.VERTICAL);
        wordmark.setGravity(Gravity.CENTER_HORIZONTAL);
        wordmark.addView(wordRow("CAMPUS ESTATE", 0xFFFFFFFF, letters));
        wordmark.addView(wordRow("LIVING", 0xFFF3D58A, letters));
        column.addView(wordmark, new LinearLayout.LayoutParams(-1, -2));

        View rule = new View(this);
        rule.setBackgroundColor(Ui.GOLD);
        rule.setScaleX(0f);
        LinearLayout.LayoutParams ruleParams = new LinearLayout.LayoutParams(Ui.dp(this, 180), Ui.dp(this, 2));
        ruleParams.topMargin = Ui.dp(this, 18);
        column.addView(rule, ruleParams);

        TextView tagline = new TextView(this);
        tagline.setText("Your Digital Campus Estate Solution");
        tagline.setTextSize(15);
        tagline.setTextColor(0xFFAAB8CC);
        tagline.setTypeface(Typeface.create("sans-serif", Typeface.NORMAL));
        tagline.setAlpha(0f);
        LinearLayout.LayoutParams tagParams = new LinearLayout.LayoutParams(-2, -2);
        tagParams.topMargin = Ui.dp(this, 14);
        column.addView(tagline, tagParams);

        boolean reducedMotion = reducedMotionEnabled();
        if (reducedMotion) {
            glow.setAlpha(1f);
            glow.setScaleX(1f);
            glow.setScaleY(1f);
            mark.setAlpha(1f);
            mark.setScaleX(1f);
            mark.setScaleY(1f);
            for (TextView letter : letters) {
                letter.setAlpha(1f);
                letter.setTranslationY(0f);
            }
            rule.setScaleX(1f);
            tagline.setAlpha(1f);
            trails.setProgress(1f);
            handler.postDelayed(this::route, MIN_HOLD_MS);
            return;
        }

        // Trails sweep in first, then the crest and wordmark settle into place.
        ValueAnimator trailReveal = ValueAnimator.ofFloat(0f, 1f);
        trailReveal.setDuration(REVEAL_MS);
        trailReveal.setInterpolator(new DecelerateInterpolator(1.4f));
        trailReveal.addUpdateListener(a -> trails.setProgress((Float) a.getAnimatedValue()));
        trailReveal.start();

        trailFlowAnimator = ValueAnimator.ofFloat(0f, 1f);
        trailFlowAnimator.setStartDelay(REVEAL_MS + 150L);
        trailFlowAnimator.setDuration(1900);
        trailFlowAnimator.setRepeatCount(ValueAnimator.INFINITE);
        trailFlowAnimator.setInterpolator(new LinearInterpolator());
        trailFlowAnimator.addUpdateListener(a -> trails.setFlowProgress((Float) a.getAnimatedValue()));
        trailFlowAnimator.start();

        glow.animate().alpha(1f).scaleX(1f).scaleY(1f)
                .setStartDelay(650).setDuration(680)
                .setInterpolator(new DecelerateInterpolator()).start();

        mark.animate().alpha(1f).scaleX(1f).scaleY(1f)
                .setStartDelay(REVEAL_MS + 180L).setDuration(1020)
                .setInterpolator(new DecelerateInterpolator(1.5f))
                .withEndAction(() -> handler.postDelayed(this::rushToApp, LOGO_HOLD_MS))
                .start();

        for (int i = 0; i < letters.size(); i++) {
            letters.get(i).animate().alpha(1f).translationY(0f)
                    .setStartDelay(REVEAL_MS + 1320L + i * 20L).setDuration(300)
                    .setInterpolator(new DecelerateInterpolator(1.5f)).start();
        }
        rule.animate().scaleX(1f).setStartDelay(REVEAL_MS + 1950L).setDuration(420)
                .setInterpolator(new DecelerateInterpolator()).start();
        tagline.animate().alpha(1f).setStartDelay(REVEAL_MS + 2420L).setDuration(420).start();
    }

    private boolean reducedMotionEnabled() {
        try {
            return Settings.Global.getFloat(getContentResolver(), Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f;
        } catch (Exception ignored) {
            return false;
        }
    }

    private LinearLayout wordRow(String word, int color, List<TextView> letters) {
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER);
        for (char ch : word.toCharArray()) {
            if (ch == ' ') {
                row.addView(new View(this), new LinearLayout.LayoutParams(Ui.dp(this, 10), 1));
                continue;
            }
            TextView letter = new TextView(this);
            letter.setText(String.valueOf(ch));
            letter.setTextSize(26);
            letter.setTextColor(color);
            letter.setTypeface(Typeface.create("sans-serif-medium", Typeface.BOLD));
            letter.setLetterSpacing(0.08f);
            letter.setAlpha(0f);
            letter.setTranslationY(Ui.dp(this, 22));
            row.addView(letter);
            letters.add(letter);
        }
        return row;
    }

    private void route() {
        if (routed) return;
        routed = true;
        handler.removeCallbacksAndMessages(null);
        Class<?> next = new SessionManager(this).isLoggedIn() ? MainActivity.class : LoginActivity.class;
        Intent intent = new Intent(this, next);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
        overridePendingTransition(android.R.anim.fade_in, android.R.anim.fade_out);
        finish();
    }

    private void rushToApp() {
        if (routed) return;
        if (logoStack == null) {
            route();
            return;
        }
        logoStack.animate().scaleX(3.8f).scaleY(3.8f).alpha(0f)
                .setDuration(RUSH_MS)
                .setInterpolator(new AccelerateInterpolator(1.15f))
                .withEndAction(this::route)
                .start();
    }

    @Override
    protected void onDestroy() {
        handler.removeCallbacksAndMessages(null);
        if (trailFlowAnimator != null) trailFlowAnimator.cancel();
        super.onDestroy();
    }

    /** A few smooth, staggered trails add depth without a particle field or heavy blur. */
    private static final class LightTrailsView extends View {
        private final Paint atmospherePaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Paint glowPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Paint trailPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Paint highlightPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Paint glintPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Path path = new Path();
        private final Path segment = new Path();
        private final PathMeasure pathMeasure = new PathMeasure();
        private final float[] glintPosition = new float[2];
        private final float density;
        private RadialGradient atmosphere;
        private float progress;
        private float flowProgress;

        LightTrailsView(Context context) {
            super(context);
            density = context.getResources().getDisplayMetrics().density;
            glowPaint.setStyle(Paint.Style.STROKE);
            glowPaint.setStrokeCap(Paint.Cap.ROUND);
            trailPaint.setStyle(Paint.Style.STROKE);
            trailPaint.setStrokeCap(Paint.Cap.ROUND);
            highlightPaint.setStyle(Paint.Style.STROKE);
            highlightPaint.setStrokeCap(Paint.Cap.ROUND);
            glintPaint.setStyle(Paint.Style.FILL);
            glintPaint.setColor(0xFFFFF4D5);
        }

        void setProgress(float value) {
            progress = value;
            invalidate();
        }

        void setFlowProgress(float value) {
            flowProgress = value;
            invalidate();
        }

        @Override
        protected void onSizeChanged(int width, int height, int oldWidth, int oldHeight) {
            super.onSizeChanged(width, height, oldWidth, oldHeight);
            atmosphere = new RadialGradient(width * 0.5f, height * 0.43f, Math.max(width, height) * 0.62f,
                    new int[]{0x261769AA, 0x101769AA, 0x000B1D38}, null, Shader.TileMode.CLAMP);
            atmospherePaint.setShader(atmosphere);
        }

        @Override
        protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            float width = getWidth();
            float height = getHeight();
            if (atmosphere != null) canvas.drawRect(0, 0, width, height, atmospherePaint);
            drawTrail(canvas, width, height, 0xFF1769AA, 0f, true);
            drawTrail(canvas, width, height, 0xFFD5AD55, 0.12f, true);
            drawTrail(canvas, width, height, 0xFF1769AA, 0.08f, false);
            drawTrail(canvas, width, height, 0xFFF3D58A, 0.2f, false);
        }

        private void drawTrail(Canvas canvas, float width, float height, int color,
                               float delay, boolean upperLeft) {
            float reveal = Math.max(0f, Math.min(1f, (progress - delay) / (1f - delay)));
            if (reveal <= 0f) return;
            path.reset();
            if (upperLeft) {
                path.moveTo(-0.08f * width, 0.12f * height);
                path.cubicTo(0.18f * width, 0.16f * height, 0.25f * width, 0.39f * height,
                        0.49f * width, 0.46f * height);
            } else {
                path.moveTo(1.08f * width, 0.91f * height);
                path.cubicTo(0.79f * width, 0.82f * height, 0.72f * width, 0.57f * height,
                        0.52f * width, 0.5f * height);
            }
            pathMeasure.setPath(path, false);
            segment.reset();
            pathMeasure.getSegment(0f, pathMeasure.getLength() * reveal, segment, true);

            float base = 0.9f + density * 0.55f;
            glowPaint.setStrokeWidth(base * 8f);
            glowPaint.setColor(color);
            glowPaint.setAlpha(24);
            canvas.drawPath(segment, glowPaint);

            trailPaint.setStrokeWidth(base * 2.2f);
            trailPaint.setColor(color);
            trailPaint.setAlpha(142);
            canvas.drawPath(segment, trailPaint);

            highlightPaint.setStrokeWidth(base * 0.7f);
            highlightPaint.setColor(color);
            highlightPaint.setAlpha(220);
            canvas.drawPath(segment, highlightPaint);

            if (reveal >= 0.98f) {
                float phase = (flowProgress + delay * 2.2f) % 1f;
                if (pathMeasure.getPosTan(pathMeasure.getLength() * phase, glintPosition, null)) {
                    glintPaint.setAlpha(42);
                    canvas.drawCircle(glintPosition[0], glintPosition[1], Ui.dp(getContext(), 7), glintPaint);
                    glintPaint.setAlpha(220);
                    canvas.drawCircle(glintPosition[0], glintPosition[1], Ui.dp(getContext(), 2.2f), glintPaint);
                }
            }
        }
    }
}
