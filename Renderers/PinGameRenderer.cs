using AAPlus.Services;
using SkiaSharp;

namespace AAPlus.Renderers;

public class PinGameRenderer
{
    private float _shakeX, _shakeTimer;
    private bool _shaking;
    private float _lcTimer, _time;
    private static readonly SKColor DimText = new(255, 255, 255, 120);

    public void Draw(SKCanvas c, SKSize sz, PinGameEngine e, float dt)
    {
        _time += dt;
        var theme = LevelThemes.GetTheme(e.CurrentLevel);

        // Arka plan
        c.Clear(SKColor.Parse("#111111"));

        float cx = sz.Width / 2f;
        float cy = sz.Height * 0.36f;

        if (_shaking)
        {
            _shakeTimer += dt;
            _shakeX = MathF.Sin(_shakeTimer * 50f) * 10f * Math.Max(0, 1f - _shakeTimer * 3f);
            if (_shakeTimer > 0.35f) { _shaking = false; _shakeX = 0; }
            cx += _shakeX;
        }

        // Arka plan efektleri
        if (theme.HasBgRings) DrawBgRings(c, cx, cy, theme);
        if (theme.HasBgParticles) DrawBgParticles(c, cx, cy, theme);

        DrawBadge(c, sz, e, theme);

        if (e.HasShield) DrawShield(c, cx, cy, e, theme);

        // Invisible mod: saplanmış iğneler fade olur
        DrawPins(c, cx, cy, e.PlacedPins, e.RotationAngle, theme.PinColor, theme, false, e);
        DrawPins(c, cx, cy, e.PrePlacedPins, e.RotationAngle,
            e.HasMovingPins ? theme.PrePinColor.WithAlpha(200) : theme.PrePinColor, theme, e.HasMovingPins, null);

        DrawCircle(c, cx, cy, e, theme);

        // SpeedBurst gösterge
        if (e.CurrentMode == GameMode.SpeedBurst && e.IsBurstActive && e.State == GameState.Playing)
        {
            using var bp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FF4444").WithAlpha(40) };
            c.DrawCircle(cx, cy, PinGameEngine.CircleRadius + 20, bp);
        }

        if (e.IsPinFlying) DrawFlyingPin(c, cx, cy, e, theme);
        if (!e.IsPinFlying && e.PinsRemaining > 0 && e.State == GameState.Playing && !e.IsPaused)
            DrawReadyPin(c, cx, sz, theme, e);

        DrawQueue(c, cx, sz, e, theme);
        DrawHud(c, sz, e, theme);

        if (e.IsPaused) DrawPauseOverlay(c, sz);

        switch (e.State)
        {
            case GameState.GameOver: DrawGameOver(c, sz, e, theme); break;
            case GameState.LevelComplete: DrawLevelComplete(c, sz, e, dt, theme); break;
            case GameState.Victory: DrawVictory(c, sz); break;
        }
    }

    // ── ARKA PLAN EFEKTLERİ ──────────────────────────────────

    private void DrawBgRings(SKCanvas c, float cx, float cy, LevelTheme t)
    {
        using var p = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f, Color = t.Accent.WithAlpha(10)
        };
        for (int i = 1; i <= 4; i++)
            c.DrawCircle(cx, cy, PinGameEngine.CircleRadius + 30 * i, p);
    }

    private void DrawBgParticles(SKCanvas c, float cx, float cy, LevelTheme t)
    {
        using var p = new SKPaint { IsAntialias = true, Color = t.Accent.WithAlpha(15) };
        for (int i = 0; i < 8; i++)
        {
            float angle = _time * 0.3f + i * MathF.PI / 4f;
            float r = 100 + 30 * MathF.Sin(_time * 0.5f + i);
            c.DrawCircle(cx + r * MathF.Cos(angle), cy + r * MathF.Sin(angle), 2f, p);
        }
    }

    // ── KALKAN HALKASI ───────────────────────────────────────

    private void DrawShield(SKCanvas c, float cx, float cy, PinGameEngine e, LevelTheme t)
    {
        float sr = PinGameEngine.CircleRadius + PinGameEngine.ShieldRadiusOffset;
        float gapHalf = e.ShieldGapAngle / 2f;
        float sDeg = e.ShieldAngle * 180f / MathF.PI;
        float ghDeg = gapHalf * 180f / MathF.PI;
        float pulse = 0.6f + 0.4f * MathF.Sin(_time * 3f);

        using var sp = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4f,
            Color = SKColor.Parse("#FF6B35").WithAlpha((byte)(pulse * 180)), StrokeCap = SKStrokeCap.Round
        };
        var rect = new SKRect(cx - sr, cy - sr, cx + sr, cy + sr);
        using var path = new SKPath();
        path.AddArc(rect, sDeg + ghDeg - 90f, 360f - ghDeg * 2f);
        c.DrawPath(path, sp);

        using var gp = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f,
            Color = SKColor.Parse("#FF6B35").WithAlpha(30),
            PathEffect = SKPathEffect.CreateDash(new[] { 4f, 6f }, 0)
        };
        using var gPath = new SKPath();
        gPath.AddArc(rect, sDeg - ghDeg - 90f, ghDeg * 2f);
        c.DrawPath(gPath, gp);
    }

    // ── İĞNELER ──────────────────────────────────────────────

    private void DrawPins(SKCanvas c, float cx, float cy, List<float> angles, float rot, SKColor col, LevelTheme t, bool moving, PinGameEngine? engine)
    {
        for (int i = 0; i < angles.Count; i++)
        {
            float da = angles[i] + rot;
            var (x1, y1) = PinGameEngine.PolarToScreen(cx, cy, PinGameEngine.CircleRadius, da);
            var (x2, y2) = PinGameEngine.PolarToScreen(cx, cy, PinGameEngine.CircleRadius + PinGameEngine.PinLength, da);

            SKColor pc = col;

            // Invisible mod: iğneler zamanla kaybolur
            if (engine != null && engine.CurrentMode == GameMode.Invisible && i < engine.PinPlaceTimes.Count)
            {
                float elapsed = engine.GameTimer - engine.PinPlaceTimes[i];
                if (elapsed > PinGameEngine.INVISIBLE_FADE_TIME)
                {
                    float fadeRatio = Math.Min(1f, (elapsed - PinGameEngine.INVISIBLE_FADE_TIME) / 1f);
                    pc = pc.WithAlpha((byte)(pc.Alpha * (1f - fadeRatio * 0.85f))); // min %15 görünürlük
                }
            }

            if (moving)
            {
                float g = 0.5f + 0.5f * MathF.Sin(_time * 2f + i * 0.8f);
                pc = pc.WithAlpha((byte)(pc.Alpha * (0.6f + 0.4f * g)));
            }

            // Pin stili
            switch (t.Pin)
            {
                case PinStyle.DotsOnly:
                    using (var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = pc })
                    {
                        c.DrawCircle(x1, y1, 3f, dp);
                        c.DrawCircle(x2, y2, t.HeadSize, dp);
                    }
                    break;
                case PinStyle.Arrow:
                    using (var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = t.PinWidth, Color = pc, StrokeCap = SKStrokeCap.Round })
                        c.DrawLine(x1, y1, x2, y2, lp);
                    float angle = da;
                    using (var ap = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = pc })
                    {
                        var path = new SKPath();
                        float s = t.HeadSize;
                        path.MoveTo(x2 + s * MathF.Sin(angle), y2 - s * MathF.Cos(angle));
                        path.LineTo(x2 + s * MathF.Sin(angle + 2.5f), y2 - s * MathF.Cos(angle + 2.5f));
                        path.LineTo(x2 + s * MathF.Sin(angle - 2.5f), y2 - s * MathF.Cos(angle - 2.5f));
                        path.Close();
                        c.DrawPath(path, ap);
                    }
                    break;
                case PinStyle.Diamond:
                    using (var lp2 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = t.PinWidth, Color = pc, StrokeCap = SKStrokeCap.Round })
                        c.DrawLine(x1, y1, x2, y2, lp2);
                    c.Save();
                    c.RotateDegrees(da * 180f / MathF.PI + 45f, x2, y2);
                    using (var rp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = pc })
                        c.DrawRect(x2 - t.HeadSize / 2f, y2 - t.HeadSize / 2f, t.HeadSize, t.HeadSize, rp);
                    c.Restore();
                    break;
                default:
                    using (var lp3 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = t.PinWidth, Color = pc, StrokeCap = SKStrokeCap.Round })
                        c.DrawLine(x1, y1, x2, y2, lp3);
                    using (var hp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = pc })
                        c.DrawCircle(x2, y2, t.HeadSize, hp);
                    break;
            }
        }
    }

    private void DrawFlyingPin(SKCanvas c, float cx, float cy, PinGameEngine e, LevelTheme t)
    {
        float tipY = cy + e.FlyingPinY;
        float botY = tipY + PinGameEngine.PinLength;
        using var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = t.PinWidth, Color = t.PinColor, StrokeCap = SKStrokeCap.Round };
        c.DrawLine(cx, tipY, cx, botY, lp);
        using var hp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = t.PinColor };
        c.DrawCircle(cx, botY, t.HeadSize, hp);
    }

    private void DrawReadyPin(SKCanvas c, float cx, SKSize sz, LevelTheme t, PinGameEngine e)
    {
        float botY = sz.Height * 0.66f;
        float tipY = botY - PinGameEngine.PinLength;
        using var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = t.PinWidth, Color = t.PinColor, StrokeCap = SKStrokeCap.Round };
        c.DrawLine(cx, tipY, cx, botY, lp);
        using var hp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = t.PinColor };
        c.DrawCircle(cx, botY, t.HeadSize, hp);

        // DoublePin gösterge: simetrik ikinci iğne sembolü
        if (e.CurrentMode == GameMode.DoublePin && e.PinsRemaining >= 2)
        {
            using var dp = new SKPaint { IsAntialias = true, Color = t.PinColor.WithAlpha(60) };
            using var dpFont = new SKFont(SKTypeface.Default, 16);
            c.DrawText("×2", cx + 20, botY, SKTextAlign.Center, dpFont, dp);
        }
    }

    // ── ÇEMBER ───────────────────────────────────────────────

    private void DrawCircle(SKCanvas c, float cx, float cy, PinGameEngine e, LevelTheme t)
    {
        bool go = e.State == GameState.GameOver;
        // Shrinking modu: daire boyutu değişir
        float r = PinGameEngine.CircleRadius * (e.CurrentMode == GameMode.Shrinking ? e.CircleScale : 1f);
        SKColor fill = go ? SKColor.Parse("#CC2222") : t.CircleFill;
        SKColor stroke = go ? SKColor.Parse("#FF4444") : t.CircleStroke;

        switch (t.Circle)
        {
            case CircleStyle.Glow:
                float g = 0.3f + 0.2f * MathF.Sin(_time * 2f);
                using (var gp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = t.Accent.WithAlpha((byte)(g * 25)) })
                    c.DrawCircle(cx, cy, r + 10, gp);
                goto case CircleStyle.Solid;

            case CircleStyle.Solid:
                using (var fp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
                    c.DrawCircle(cx, cy, r, fp);
                using (var sp2 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = stroke })
                    c.DrawCircle(cx, cy, r, sp2);
                break;

            case CircleStyle.Hollow:
                using (var sp3 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3f, Color = stroke })
                    c.DrawCircle(cx, cy, r, sp3);
                break;

            case CircleStyle.Rings:
                using (var fp2 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
                    c.DrawCircle(cx, cy, r, fp2);
                for (int i = 1; i <= 3; i++)
                    using (var rp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = stroke.WithAlpha((byte)(40 - i * 10)) })
                        c.DrawCircle(cx, cy, r - i * 10, rp);
                using (var sp4 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = stroke })
                    c.DrawCircle(cx, cy, r, sp4);
                break;

            case CircleStyle.Dotted:
                using (var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
                    c.DrawCircle(cx, cy, r, dp);
                using (var dotp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f, Color = stroke,
                    PathEffect = SKPathEffect.CreateDash(new[] { 5f, 8f }, _time * 20f) })
                    c.DrawCircle(cx, cy, r, dotp);
                break;

            case CircleStyle.Pulsing:
                float pulse = 1f + 0.04f * MathF.Sin(_time * 4f);
                using (var fp3 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
                    c.DrawCircle(cx, cy, r * pulse, fp3);
                using (var sp5 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = stroke })
                    c.DrawCircle(cx, cy, r * pulse, sp5);
                break;

            case CircleStyle.DoubleRing:
                using (var fp4 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
                    c.DrawCircle(cx, cy, r, fp4);
                using (var sp6 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = stroke })
                    c.DrawCircle(cx, cy, r, sp6);
                using (var sp7 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = stroke.WithAlpha(40) })
                    c.DrawCircle(cx, cy, r - 8, sp7);
                break;

            case CircleStyle.CrossHatch:
                using (var fp5 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
                    c.DrawCircle(cx, cy, r, fp5);
                c.Save();
                c.ClipRoundRect(new SKRoundRect(new SKRect(cx - r, cy - r, cx + r, cy + r), r), SKClipOperation.Intersect);
                using (var hp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 0.5f, Color = stroke.WithAlpha(25) })
                    for (float i = -r; i <= r; i += 12)
                    {
                        c.DrawLine(cx + i, cy - r, cx + i + r, cy + r, hp);
                        c.DrawLine(cx + i, cy - r, cx + i - r, cy + r, hp);
                    }
                c.Restore();
                using (var sp8 = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = stroke })
                    c.DrawCircle(cx, cy, r, sp8);
                break;
        }

        // Level numarası
        SKColor numCol = e.IsBossLevel ? SKColor.Parse("#FFD700") : t.Accent;
        using var num = new SKPaint { IsAntialias = true, Color = numCol };
        using var numFont = new SKFont(
            SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            e.IsBossLevel ? 22 : 26);
        c.DrawText(e.CurrentLevel.ToString(), cx, cy + 8, SKTextAlign.Center, numFont, num);

        if (e.IsBossLevel)
        {
            using var bp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FFD700").WithAlpha(180) };
            using var bpFont = new SKFont(
                SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 9);
            c.DrawText("BOSS", cx, cy + 22, SKTextAlign.Center, bpFont, bp);
        }
    }

    // ── KUYRUK ───────────────────────────────────────────────

    private void DrawQueue(SKCanvas c, float cx, SKSize sz, PinGameEngine e, LevelTheme t)
    {
        float startY = sz.Height * 0.73f;
        int rem = e.PinsRemaining - (e.IsPinFlying ? 1 : 0) - 1;
        for (int i = 0; i < Math.Min(rem, 6); i++)
        {
            byte a = (byte)(255 * Math.Max(0.15f, 1f - i * 0.15f));
            using var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = t.Accent.WithAlpha((byte)(a / 3)) };
            c.DrawCircle(cx, startY + i * 22f, 5f, dp);
        }
        if (rem > 6)
        {
            using var tp = new SKPaint { IsAntialias = true, Color = t.Accent.WithAlpha(60) };
            using var tpFont = new SKFont(SKTypeface.Default, 12);
            c.DrawText($"+{rem - 6}", cx, startY + 6 * 22f + 14, SKTextAlign.Center, tpFont, tp);
        }
    }

    // ── HUD + BADGE ──────────────────────────────────────────

    private void DrawBadge(SKCanvas c, SKSize sz, PinGameEngine e, LevelTheme t)
    {
        var col = e.IsBossLevel ? SKColor.Parse("#FFD700") : SKColor.Parse(PinGameEngine.DifficultyColor(e.CurrentDifficulty));
        string name = e.IsBossLevel ? $"⚔ BOSS — {t.Name}" : $"{PinGameEngine.DifficultyName(e.CurrentDifficulty)} — {t.Name}";
        float bw = e.IsBossLevel ? 180 : 160;
        float bx = (sz.Width - bw) / 2f, by = 50;

        using var bg = new SKPaint { IsAntialias = true, Color = col.WithAlpha(20) };
        c.DrawRoundRect(bx, by, bw, 24, 12, 12, bg);
        using var bd = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = col.WithAlpha(80) };
        c.DrawRoundRect(bx, by, bw, 24, 12, 12, bd);
        using var tp2 = new SKPaint { IsAntialias = true, Color = col };
        using var tp2Font = new SKFont(
            SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 11);
        c.DrawText(name, sz.Width / 2f, by + 16, SKTextAlign.Center, tp2Font, tp2);

        // Mod + davranış + mekanik ikonları
        if (e.State == GameState.Playing)
        {
            var icons = new List<string>();

            // RotationBehavior ikonu
            string behaviorIcon = e.CurrentBehavior switch
            {
                RotationBehavior.Accelerating => "🚀",
                RotationBehavior.Decelerating => "🐌",
                RotationBehavior.SpeedBurst => "⚡",
                RotationBehavior.SlowMotion => "🕐",
                RotationBehavior.FakeReverse => "🔄",
                RotationBehavior.Chaos => "🌀",
                _ => ""
            };
            if (!string.IsNullOrEmpty(behaviorIcon)) icons.Add(behaviorIcon);

            // Oyun modu ikonu
            string modeIcon = e.CurrentMode switch
            {
                GameMode.SpeedBurst => "⚡",
                GameMode.DoublePin => "×2",
                GameMode.Invisible => "👻",
                GameMode.Shrinking => "📐",
                _ => ""
            };
            if (!string.IsNullOrEmpty(modeIcon)) icons.Add(modeIcon);
            if (e.HasShield) icons.Add("🛡");
            if (e.HasMovingPins) icons.Add("↔");

            if (icons.Count > 0)
            {
                using var ip = new SKPaint { IsAntialias = true, Color = DimText };
                using var ipFont = new SKFont(SKTypeface.Default, 11);
                c.DrawText(string.Join("  ", icons), sz.Width / 2f, by + 36, SKTextAlign.Center, ipFont, ip);
            }
        }
    }

    private void DrawHud(SKCanvas c, SKSize sz, PinGameEngine e, LevelTheme t)
    {
        using var hudFont = new SKFont(SKTypeface.Default, 12);
        using var lp = new SKPaint { IsAntialias = true, Color = DimText };
        c.DrawText($"LEVEL {e.CurrentLevel}/{PinGameEngine.MaxLevel}", 20, 28, SKTextAlign.Left, hudFont, lp);
        using var rp = new SKPaint { IsAntialias = true, Color = DimText };
        c.DrawText($"BEST: {e.HighScore}", sz.Width - 20, 28, SKTextAlign.Right, hudFont, rp);
        using var sp = new SKPaint { IsAntialias = true, Color = t.Accent.WithAlpha(180) };
        c.DrawText($"SCORE: {e.Score}", sz.Width / 2f, 28, SKTextAlign.Center, hudFont, sp);
        using var ppFont = new SKFont(SKTypeface.Default, 20);
        using var pp = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 60) };
        c.DrawText("⏸", sz.Width - 18, 60, SKTextAlign.Right, ppFont, pp);
    }

    // ── OVERLAY'LAR ──────────────────────────────────────────

    private void DrawPauseOverlay(SKCanvas c, SKSize sz)
    {
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 180) };
        c.DrawRect(0, 0, sz.Width, sz.Height, ov);
        float cy = sz.Height / 2f;
        using var tp = new SKPaint { IsAntialias = true, Color = SKColors.White };
        using var tpFont = new SKFont(
            SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 30);
        c.DrawText("DURAKLATILDI", sz.Width / 2f, cy - 10, SKTextAlign.Center, tpFont, tp);
        using var sp = new SKPaint { IsAntialias = true, Color = DimText };
        using var spFont = new SKFont(SKTypeface.Default, 14);
        c.DrawText("Devam etmek için dokun", sz.Width / 2f, cy + 25, SKTextAlign.Center, spFont, sp);
    }

    private void DrawGameOver(SKCanvas c, SKSize sz, PinGameEngine e, LevelTheme t)
    {
        if (!_shaking) { _shaking = true; _shakeTimer = 0; }
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 180) };
        c.DrawRect(0, 0, sz.Width, sz.Height, ov);
        float cy = sz.Height / 2f;

        using var tp = new SKPaint { IsAntialias = true, Color = SKColors.White };
        using var tpFont = new SKFont(
            SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 34);
        c.DrawText("GAME OVER", sz.Width / 2f, cy - 45, SKTextAlign.Center, tpFont, tp);

        using var sp = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 180) };
        using var spFont = new SKFont(SKTypeface.Default, 16);
        c.DrawText($"Level {e.CurrentLevel}  ·  Score {e.Score}", sz.Width / 2f, cy - 10, SKTextAlign.Center, spFont, sp);

        float pulse = 0.5f + 0.5f * MathF.Sin(_time * 3f);
        using var rp = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, (byte)(pulse * 160)) };
        using var rpFont = new SKFont(SKTypeface.Default, 14);
        c.DrawText("Tekrar denemek için dokun", sz.Width / 2f, cy + 50, SKTextAlign.Center, rpFont, rp);
    }

    private void DrawLevelComplete(SKCanvas c, SKSize sz, PinGameEngine e, float dt, LevelTheme t)
    {
        _lcTimer += dt;
        float a = Math.Min(1f, _lcTimer * 4f);
        using var fp = new SKPaint { Color = t.Accent.WithAlpha((byte)(a * 30)) };
        c.DrawRect(0, 0, sz.Width, sz.Height, fp);
        float scale = 0.5f + a * 0.5f;
        string sym = e.IsBossLevel ? "⚔" : "✓";
        using var cp = new SKPaint { IsAntialias = true, Color = t.Accent.WithAlpha((byte)(a * 255)) };
        using var cpFont = new SKFont(
            SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 48 * scale);
        c.DrawText(sym, sz.Width / 2f, sz.Height * 0.55f, SKTextAlign.Center, cpFont, cp);
    }

    private void DrawVictory(SKCanvas c, SKSize sz)
    {
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 200) };
        c.DrawRect(0, 0, sz.Width, sz.Height, ov);
        float cy = sz.Height / 2f;
        using var cp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FFD700") };
        using var cpFont = new SKFont(SKTypeface.Default, 50);
        c.DrawText("🏆", sz.Width / 2f, cy - 30, SKTextAlign.Center, cpFont, cp);
        using var tp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FFD700") };
        using var tpFont = new SKFont(
            SKTypeface.FromFamilyName("Helvetica Neue", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 28);
        c.DrawText("TEBRİKLER", sz.Width / 2f, cy + 15, SKTextAlign.Center, tpFont, tp);
        using var sp = new SKPaint { IsAntialias = true, Color = SKColors.White };
        using var spFont = new SKFont(SKTypeface.Default, 16);
        c.DrawText("250 seviye tamamlandı!", sz.Width / 2f, cy + 45, SKTextAlign.Center, spFont, sp);
    }

    public bool IsPauseHit(float x, float y, SKSize sz) => x > sz.Width - 50 && y < 75;
    public void ResetLevelTimer() => _lcTimer = 0;
}
