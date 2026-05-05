using AAPlus.Services;
using SkiaSharp;

namespace AAPlus.Renderers;

/// <summary>
/// "aa" oyununa sadık minimalist renderer.
/// Siyah arka plan, beyaz iğneler, temiz tipografi.
/// </summary>
public class PinGameRenderer
{
    private float _shakeX, _shakeTimer;
    private bool _shaking;
    private float _lcTimer; // level complete timer
    private float _time;

    // ─── Renkler (siyah-beyaz minimalist) ────────────────────
    private static readonly SKColor BgColor = SKColor.Parse("#111111");
    private static readonly SKColor CircleColor = SKColor.Parse("#222222");
    private static readonly SKColor PinColor = SKColors.White;
    private static readonly SKColor PrePinColor = SKColor.Parse("#555555");
    private static readonly SKColor QueueColor = SKColor.Parse("#444444");
    private static readonly SKColor TextColor = SKColors.White;
    private static readonly SKColor DimText = new(255, 255, 255, 120);

    public void Draw(SKCanvas c, SKSize sz, PinGameEngine e, float dt)
    {
        _time += dt;
        c.Clear(BgColor);

        float cx = sz.Width / 2f;
        float cy = sz.Height * 0.36f;

        // Shake efekti (game over)
        if (_shaking)
        {
            _shakeTimer += dt;
            _shakeX = MathF.Sin(_shakeTimer * 50f) * 10f * Math.Max(0, 1f - _shakeTimer * 3f);
            if (_shakeTimer > 0.35f) { _shaking = false; _shakeX = 0; }
            cx += _shakeX;
        }

        // Zorluk badge'i
        DrawBadge(c, sz, e);

        // Saplanmış iğneler (çubuklar)
        DrawPins(c, cx, cy, e.PlacedPins, e.RotationAngle, PinColor);
        DrawPins(c, cx, cy, e.PrePlacedPins, e.RotationAngle, PrePinColor);

        // Merkez daire
        DrawCircle(c, cx, cy, e);

        // Uçan iğne
        if (e.IsPinFlying)
            DrawFlyingPin(c, cx, cy, e);

        // Hazır iğne (alttan)
        if (!e.IsPinFlying && e.PinsRemaining > 0 && e.State == GameState.Playing && !e.IsPaused)
            DrawReadyPin(c, cx, sz);

        // Kuyruk (kalan iğneler)
        DrawQueue(c, cx, sz, e);

        // Üst bilgi
        DrawHud(c, sz, e);

        // Duraklatma overlay
        if (e.IsPaused)
            DrawPauseOverlay(c, sz);

        // Overlay ekranlar
        switch (e.State)
        {
            case GameState.GameOver: DrawGameOver(c, sz, e); break;
            case GameState.LevelComplete: DrawLevelComplete(c, sz, e, dt); break;
            case GameState.Victory: DrawVictory(c, sz); break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  İĞNELER (çubuk + uç)
    // ═══════════════════════════════════════════════════════════

    private void DrawPins(SKCanvas c, float cx, float cy, List<float> angles, float rot, SKColor col)
    {
        foreach (var a in angles)
        {
            float da = a + rot;
            float sin = MathF.Sin(da), cos = MathF.Cos(da);

            // İğne: daireden dışarı doğru uzanan çubuk
            float x1 = cx + sin * PinGameEngine.CircleRadius;
            float y1 = cy - cos * PinGameEngine.CircleRadius;
            float x2 = cx + sin * (PinGameEngine.CircleRadius + PinGameEngine.PinLength);
            float y2 = cy - cos * (PinGameEngine.CircleRadius + PinGameEngine.PinLength);

            // Çubuk
            using var line = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f, Color = col, StrokeCap = SKStrokeCap.Round
            };
            c.DrawLine(x1, y1, x2, y2, line);

            // İğne ucu (küçük daire)
            using var head = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Fill, Color = col
            };
            c.DrawCircle(x2, y2, PinGameEngine.PinHeadRadius, head);
        }
    }

    private void DrawFlyingPin(SKCanvas c, float cx, float cy, PinGameEngine e)
    {
        // İğne alttan merkeze doğru uçuyor
        float tipY = cy + e.FlyingPinY;                          // iğne ucu (üst)
        float bottomY = tipY + PinGameEngine.PinLength;           // iğne altı

        using var line = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f, Color = PinColor, StrokeCap = SKStrokeCap.Round
        };
        c.DrawLine(cx, tipY, cx, bottomY, line);

        using var head = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Fill, Color = PinColor
        };
        c.DrawCircle(cx, bottomY, PinGameEngine.PinHeadRadius, head);
    }

    private void DrawReadyPin(SKCanvas c, float cx, SKSize sz)
    {
        float bottomY = sz.Height * 0.66f;
        float tipY = bottomY - PinGameEngine.PinLength;

        using var line = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f, Color = PinColor, StrokeCap = SKStrokeCap.Round
        };
        c.DrawLine(cx, tipY, cx, bottomY, line);

        using var head = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Fill, Color = PinColor
        };
        c.DrawCircle(cx, bottomY, PinGameEngine.PinHeadRadius, head);
    }

    // ═══════════════════════════════════════════════════════════
    //  MERKEZ DAİRE
    // ═══════════════════════════════════════════════════════════

    private void DrawCircle(SKCanvas c, float cx, float cy, PinGameEngine e)
    {
        bool go = e.State == GameState.GameOver;

        // Ana daire
        using var fill = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Fill,
            Color = go ? SKColor.Parse("#CC2222") : CircleColor
        };
        c.DrawCircle(cx, cy, PinGameEngine.CircleRadius, fill);

        // İnce beyaz kenarlık
        using var stroke = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = go ? SKColor.Parse("#FF4444") : new SKColor(255, 255, 255, 40)
        };
        c.DrawCircle(cx, cy, PinGameEngine.CircleRadius, stroke);

        // Seviye numarası
        using var num = new SKPaint
        {
            IsAntialias = true, Color = TextColor,
            TextSize = 26, TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(e.CurrentLevel.ToString(), cx, cy + 9, num);
    }

    // ═══════════════════════════════════════════════════════════
    //  KUYRUK (kalan iğneler)
    // ═══════════════════════════════════════════════════════════

    private void DrawQueue(SKCanvas c, float cx, SKSize sz, PinGameEngine e)
    {
        float startY = sz.Height * 0.73f;
        int rem = e.PinsRemaining - (e.IsPinFlying ? 1 : 0) - 1;

        for (int i = 0; i < Math.Min(rem, 6); i++)
        {
            byte alpha = (byte)(255 * Math.Max(0.15f, 1f - i * 0.15f));
            using var dot = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Fill,
                Color = QueueColor.WithAlpha(alpha)
            };
            c.DrawCircle(cx, startY + i * 22f, 5f, dot);
        }

        if (rem > 6)
        {
            using var txt = new SKPaint
            {
                IsAntialias = true, Color = QueueColor,
                TextSize = 12, TextAlign = SKTextAlign.Center
            };
            c.DrawText($"+{rem - 6}", cx, startY + 6 * 22f + 14, txt);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HUD (üst bilgi + zorluk badge)
    // ═══════════════════════════════════════════════════════════

    private void DrawBadge(SKCanvas c, SKSize sz, PinGameEngine e)
    {
        var col = SKColor.Parse(PinGameEngine.DifficultyColor(e.CurrentDifficulty));
        string name = PinGameEngine.DifficultyName(e.CurrentDifficulty);

        float bw = 110, bx = (sz.Width - bw) / 2f, by = 50;

        using var bg = new SKPaint { IsAntialias = true, Color = col.WithAlpha(20) };
        c.DrawRoundRect(bx, by, bw, 24, 12, 12, bg);

        using var border = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f, Color = col.WithAlpha(80)
        };
        c.DrawRoundRect(bx, by, bw, 24, 12, 12, border);

        using var txt = new SKPaint
        {
            IsAntialias = true, Color = col, TextSize = 11,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(name, sz.Width / 2f, by + 16, txt);
    }

    private void DrawHud(SKCanvas c, SKSize sz, PinGameEngine e)
    {
        using var lp = new SKPaint { IsAntialias = true, Color = DimText, TextSize = 12 };
        c.DrawText($"LEVEL {e.CurrentLevel}/{PinGameEngine.MaxLevel}", 20, 28, lp);

        using var rp = new SKPaint
        {
            IsAntialias = true, Color = DimText, TextSize = 12,
            TextAlign = SKTextAlign.Right
        };
        c.DrawText($"BEST: {e.HighScore}", sz.Width - 20, 28, rp);

        // Duraklatma butonu (sağ üst)
        using var pauseP = new SKPaint
        {
            IsAntialias = true, Color = new SKColor(255, 255, 255, 60),
            TextSize = 20, TextAlign = SKTextAlign.Right
        };
        c.DrawText("⏸", sz.Width - 18, 60, pauseP);
    }

    // ═══════════════════════════════════════════════════════════
    //  OVERLAY EKRANLAR
    // ═══════════════════════════════════════════════════════════

    private void DrawPauseOverlay(SKCanvas c, SKSize sz)
    {
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 180) };
        c.DrawRect(0, 0, sz.Width, sz.Height, ov);

        float cy = sz.Height / 2f;

        using var t = new SKPaint
        {
            IsAntialias = true, Color = TextColor, TextSize = 30,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("DURAKLATILDI", sz.Width / 2f, cy - 10, t);

        using var sub = new SKPaint
        {
            IsAntialias = true, Color = DimText, TextSize = 14,
            TextAlign = SKTextAlign.Center
        };
        c.DrawText("Devam etmek için dokun", sz.Width / 2f, cy + 25, sub);
    }

    private void DrawGameOver(SKCanvas c, SKSize sz, PinGameEngine e)
    {
        if (!_shaking) { _shaking = true; _shakeTimer = 0; }

        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 180) };
        c.DrawRect(0, 0, sz.Width, sz.Height, ov);

        float cy = sz.Height / 2f;

        // Başlık
        using var t = new SKPaint
        {
            IsAntialias = true, Color = TextColor, TextSize = 34,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("GAME OVER", sz.Width / 2f, cy - 45, t);

        // Skor
        using var s = new SKPaint
        {
            IsAntialias = true, Color = new SKColor(255, 255, 255, 180),
            TextSize = 16, TextAlign = SKTextAlign.Center
        };
        c.DrawText($"Level {e.CurrentLevel}  ·  Score {e.Score}", sz.Width / 2f, cy - 10, s);

        // Zorluk
        var dc = SKColor.Parse(PinGameEngine.DifficultyColor(e.CurrentDifficulty));
        using var d = new SKPaint
        {
            IsAntialias = true, Color = dc, TextSize = 13,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(PinGameEngine.DifficultyName(e.CurrentDifficulty), sz.Width / 2f, cy + 15, d);

        // Tekrar dene (yanıp söner)
        float pulse = 0.5f + 0.5f * MathF.Sin(_time * 3f);
        using var r = new SKPaint
        {
            IsAntialias = true, Color = new SKColor(255, 255, 255, (byte)(pulse * 160)),
            TextSize = 14, TextAlign = SKTextAlign.Center
        };
        c.DrawText("Tekrar denemek için dokun", sz.Width / 2f, cy + 60, r);
    }

    private void DrawLevelComplete(SKCanvas c, SKSize sz, PinGameEngine e, float dt)
    {
        _lcTimer += dt;
        float t = Math.Min(1f, _lcTimer * 4f);
        var dc = SKColor.Parse(PinGameEngine.DifficultyColor(e.CurrentDifficulty));

        using var flash = new SKPaint { Color = dc.WithAlpha((byte)(t * 30)) };
        c.DrawRect(0, 0, sz.Width, sz.Height, flash);

        float scale = 0.5f + t * 0.5f;
        using var check = new SKPaint
        {
            IsAntialias = true, Color = dc.WithAlpha((byte)(t * 255)),
            TextSize = 48 * scale, TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("✓", sz.Width / 2f, sz.Height * 0.55f, check);
    }

    private void DrawVictory(SKCanvas c, SKSize sz)
    {
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 200) };
        c.DrawRect(0, 0, sz.Width, sz.Height, ov);

        float cy = sz.Height / 2f;

        using var cup = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#FFD700"),
            TextSize = 50, TextAlign = SKTextAlign.Center
        };
        c.DrawText("🏆", sz.Width / 2f, cy - 30, cup);

        using var t = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#FFD700"),
            TextSize = 28, TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("TEBRİKLER", sz.Width / 2f, cy + 15, t);

        using var sub = new SKPaint
        {
            IsAntialias = true, Color = TextColor, TextSize = 16,
            TextAlign = SKTextAlign.Center
        };
        c.DrawText("500 seviye tamamlandı!", sz.Width / 2f, cy + 45, sub);
    }

    // ─── Pause butonu hit test ──────────────────────────────
    public bool IsPauseHit(float x, float y, SKSize sz)
        => x > sz.Width - 50 && y < 75;

    public void ResetLevelTimer() => _lcTimer = 0;
}
