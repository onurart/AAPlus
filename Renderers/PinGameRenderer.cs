using AAPlus.Services;
using SkiaSharp;

namespace AAPlus.Renderers;

public class PinGameRenderer
{
    private float _shakeOffset, _shakeTime;
    private bool _isShaking;
    private float _levelCompleteTimer, _globalTime;

    private static readonly SKColor BgColor = SKColor.Parse("#FAFAFA");
    private static readonly SKColor DotColor = SKColor.Parse("#1A1A1A");
    private static readonly SKColor PreDotColor = SKColor.Parse("#444444");
    private static readonly SKColor QueueDotColor = SKColor.Parse("#BBBBBB");

    public void Draw(SKCanvas canvas, SKSize size, PinGameEngine engine, float dt)
    {
        _globalTime += dt;
        canvas.Clear(BgColor);

        float cx = size.Width / 2f, cy = size.Height * 0.36f;

        if (_isShaking)
        {
            _shakeTime += dt;
            _shakeOffset = MathF.Sin(_shakeTime * 55f) * 10f * Math.Max(0, 1f - _shakeTime * 2.5f);
            if (_shakeTime > 0.4f) { _isShaking = false; _shakeOffset = 0; }
            cx += _shakeOffset;
        }

        DrawDifficultyBadge(canvas, size, engine);
        DrawDots(canvas, cx, cy, engine.PlacedPinAngles, engine.RotationAngle, DotColor);
        DrawDots(canvas, cx, cy, engine.PrePlacedPinAngles, engine.RotationAngle, PreDotColor);
        DrawCircle(canvas, cx, cy, engine);
        if (engine.IsPinFlying) DrawFlyingDot(canvas, cx, cy, engine);
        DrawReadyDot(canvas, size, engine);
        DrawQueue(canvas, size, engine);
        DrawInfo(canvas, size, engine);

        switch (engine.State)
        {
            case GameState.GameOver: DrawGameOver(canvas, size, engine); break;
            case GameState.LevelComplete: DrawLevelComplete(canvas, size, engine, dt); break;
            case GameState.Victory: DrawVictory(canvas, size); break;
        }
    }

    private void DrawDifficultyBadge(SKCanvas canvas, SKSize size, PinGameEngine engine)
    {
        var color = SKColor.Parse(PinGameEngine.GetDifficultyColor(engine.CurrentDifficulty));
        string name = PinGameEngine.GetDifficultyName(engine.CurrentDifficulty);
        float bw = 120, bx = (size.Width - bw) / 2f, by = 48;

        using var bg = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = color.WithAlpha(25) };
        canvas.DrawRoundRect(bx, by, bw, 26, 13, 13, bg);
        using var border = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f, Color = color.WithAlpha(100) };
        canvas.DrawRoundRect(bx, by, bw, 26, 13, 13, border);
        using var txt = new SKPaint { IsAntialias = true, Color = color, TextSize = 12, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText(name.ToUpper(), size.Width / 2f, by + 17, txt);
    }

    private void DrawCircle(SKCanvas canvas, float cx, float cy, PinGameEngine engine)
    {
        bool go = engine.State == GameState.GameOver;
        var dc = SKColor.Parse(PinGameEngine.GetDifficultyColor(engine.CurrentDifficulty));
        if (!go) { using var g = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = dc.WithAlpha(40) }; canvas.DrawCircle(cx, cy, PinGameEngine.CircleRadius + 5, g); }
        using var cp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = go ? SKColor.Parse("#E53935") : DotColor };
        canvas.DrawCircle(cx, cy, PinGameEngine.CircleRadius, cp);
        using var np = new SKPaint { IsAntialias = true, Color = SKColors.White, TextSize = 28, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText(engine.CurrentLevel.ToString(), cx, cy + 10, np);
    }

    private void DrawDots(SKCanvas canvas, float cx, float cy, List<float> angles, float rot, SKColor col)
    {
        foreach (var a in angles)
        {
            float da = a + rot;
            float x1 = cx + MathF.Sin(da) * PinGameEngine.CircleRadius, y1 = cy - MathF.Cos(da) * PinGameEngine.CircleRadius;
            float x2 = cx + MathF.Sin(da) * (PinGameEngine.CircleRadius + PinGameEngine.PinLength), y2 = cy - MathF.Cos(da) * (PinGameEngine.CircleRadius + PinGameEngine.PinLength);
            using var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f, Color = col, StrokeCap = SKStrokeCap.Round };
            canvas.DrawLine(x1, y1, x2, y2, lp);
            using var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = col };
            canvas.DrawCircle(x2, y2, 14f, dp);
        }
    }

    private void DrawFlyingDot(SKCanvas canvas, float cx, float cy, PinGameEngine engine)
    {
        float by = cy + engine.FlyingPinY, ty = by - PinGameEngine.PinLength;
        using var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f, Color = DotColor, StrokeCap = SKStrokeCap.Round };
        canvas.DrawLine(cx, ty, cx, by, lp);
        using var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = DotColor };
        canvas.DrawCircle(cx, by, 14f, dp);
    }

    private void DrawReadyDot(SKCanvas canvas, SKSize size, PinGameEngine engine)
    {
        if (engine.IsPinFlying || engine.PinsRemaining <= 0 || engine.State != GameState.Playing) return;
        float cx = size.Width / 2f, ry = size.Height * 0.64f;
        using var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f, Color = DotColor, StrokeCap = SKStrokeCap.Round };
        canvas.DrawLine(cx, ry - PinGameEngine.PinLength, cx, ry, lp);
        using var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = DotColor };
        canvas.DrawCircle(cx, ry, 14f, dp);
    }

    private void DrawQueue(SKCanvas canvas, SKSize size, PinGameEngine engine)
    {
        float cx = size.Width / 2f, sy = size.Height * 0.73f;
        int rem = engine.PinsRemaining - (engine.IsPinFlying ? 1 : 0) - 1;
        for (int i = 0; i < Math.Min(rem, 8); i++)
        {
            byte alpha = (byte)(255 * Math.Max(0.15f, 1f - i * 0.12f));
            using var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = QueueDotColor.WithAlpha(alpha) };
            canvas.DrawCircle(cx, sy + i * 24f, 9f, dp);
        }
        if (rem > 8)
        {
            using var cp = new SKPaint { IsAntialias = true, Color = QueueDotColor, TextSize = 13, TextAlign = SKTextAlign.Center };
            canvas.DrawText($"+{rem - 8}", cx, sy + 8 * 24f + 14, cp);
        }
    }

    private void DrawInfo(SKCanvas canvas, SKSize size, PinGameEngine engine)
    {
        using var lp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#888888"), TextSize = 13 };
        canvas.DrawText($"SEVİYE {engine.CurrentLevel} / {PinGameEngine.MaxLevel}", 20, 28, lp);
        using var hp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#888888"), TextSize = 13, TextAlign = SKTextAlign.Right };
        canvas.DrawText($"EN İYİ: {engine.HighScore}", size.Width - 20, 28, hp);
    }

    private void DrawGameOver(SKCanvas canvas, SKSize size, PinGameEngine engine)
    {
        if (!_isShaking) { _isShaking = true; _shakeTime = 0; }
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 160) };
        canvas.DrawRect(0, 0, size.Width, size.Height, ov);
        float cy = size.Height / 2f;
        using var tp = new SKPaint { IsAntialias = true, Color = SKColors.White, TextSize = 36, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText("GAME OVER", size.Width / 2f, cy - 40, tp);
        using var ip = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 190), TextSize = 18, TextAlign = SKTextAlign.Center };
        canvas.DrawText($"Seviye {engine.CurrentLevel}  •  Skor {engine.Score}", size.Width / 2f, cy, ip);
        var dc = SKColor.Parse(PinGameEngine.GetDifficultyColor(engine.CurrentDifficulty));
        using var dp = new SKPaint { IsAntialias = true, Color = dc, TextSize = 15, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText(PinGameEngine.GetDifficultyName(engine.CurrentDifficulty).ToUpper(), size.Width / 2f, cy + 30, dp);
        float pulse = 0.7f + 0.3f * MathF.Sin(_globalTime * 3f);
        using var rp = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, (byte)(pulse * 180)), TextSize = 15, TextAlign = SKTextAlign.Center };
        canvas.DrawText("Tekrar denemek için dokun", size.Width / 2f, cy + 70, rp);
    }

    private void DrawLevelComplete(SKCanvas canvas, SKSize size, PinGameEngine engine, float dt)
    {
        _levelCompleteTimer += dt;
        float t = Math.Min(1f, _levelCompleteTimer * 4f);
        var dc = SKColor.Parse(PinGameEngine.GetDifficultyColor(engine.CurrentDifficulty));
        using var fp = new SKPaint { Color = dc.WithAlpha((byte)(t * 40)) };
        canvas.DrawRect(0, 0, size.Width, size.Height, fp);
        using var cp = new SKPaint { IsAntialias = true, Color = dc.WithAlpha((byte)(t * 255)), TextSize = 50 * (0.5f + t * 0.5f), TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText("✓", size.Width / 2f, size.Height * 0.55f, cp);
    }

    private void DrawVictory(SKCanvas canvas, SKSize size)
    {
        using var ov = new SKPaint { Color = new SKColor(0, 0, 0, 190) };
        canvas.DrawRect(0, 0, size.Width, size.Height, ov);
        float cy = size.Height / 2f;
        using var tp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FFD700"), TextSize = 32, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText("🏆 TEBRİKLER!", size.Width / 2f, cy, tp);
        using var sp = new SKPaint { IsAntialias = true, Color = SKColors.White, TextSize = 18, TextAlign = SKTextAlign.Center };
        canvas.DrawText("500 seviyeyi tamamladın!", size.Width / 2f, cy + 35, sp);
    }

    public void ResetLevelCompleteTimer() => _levelCompleteTimer = 0;
}
