using AAPlus.ViewModels;
using SkiaSharp;

namespace AAPlus.Renderers;

public enum MenuButton { None, Play, Continue, Settings }

public class MainMenuRenderer
{
    private SKRect _playRect, _continueRect, _settingsRect;
    private readonly float[] _orbits = { 0, 1.2f, 2.4f, 3.6f, 4.8f, 6.0f };

    public void Draw(SKCanvas canvas, SKSize size, MainMenuViewModel vm, float time)
    {
        canvas.Clear(SKColor.Parse("#FAFAFA"));
        float cx = size.Width / 2f;

        // Dekoratif noktalar
        float oy = size.Height * 0.28f;
        for (int i = 0; i < 6; i++)
        {
            float a = _orbits[i] + time * (0.5f + i * 0.08f);
            float x = cx + MathF.Sin(a) * 85f, y = oy - MathF.Cos(a) * 85f;
            using var lp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = new SKColor(26, 26, 26, 40) };
            canvas.DrawLine(cx, oy, x, y, lp);
            using var dp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = new SKColor(26, 26, 26, 60) };
            canvas.DrawCircle(x, y, 10f, dp);
        }

        // Logo
        using var tp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1A1A1A"), TextSize = 36, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.Thin, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText("aa", cx, size.Height * 0.15f, tp);

        // Merkez daire
        using var cp = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColor.Parse("#1A1A1A") };
        canvas.DrawCircle(cx, oy, 45f, cp);
        using var np = new SKPaint { IsAntialias = true, Color = SKColors.White, TextSize = 24, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText(vm.BestLevel.ToString(), cx, oy + 8, np);
        using var lbl = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 150), TextSize = 10, TextAlign = SKTextAlign.Center };
        canvas.DrawText("LEVEL", cx, oy + 22, lbl);

        // Butonlar
        float bw = Math.Min(220, size.Width * 0.55f), btnY = size.Height * 0.52f;

        // OYNA
        _playRect = new SKRect(cx - bw / 2, btnY, cx + bw / 2, btnY + 48);
        using var pb = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColor.Parse("#1A1A1A") };
        canvas.DrawRoundRect(_playRect, 24, 24, pb);
        using var pt = new SKPaint { IsAntialias = true, Color = SKColors.White, TextSize = 17, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        canvas.DrawText("▶  OYNA", cx, btnY + 30, pt);

        btnY += 60;
        if (vm.CanContinue)
        {
            _continueRect = new SKRect(cx - bw / 2, btnY, cx + bw / 2, btnY + 44);
            using var cb = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, Color = SKColor.Parse("#1A1A1A") };
            canvas.DrawRoundRect(_continueRect, 22, 22, cb);
            using var ct = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1A1A1A"), TextSize = 15, TextAlign = SKTextAlign.Center };
            canvas.DrawText($"DEVAM ET  ›  Seviye {vm.LastLevel}", cx, btnY + 28, ct);
            btnY += 56;
        }

        // Ses
        _settingsRect = new SKRect(cx - bw / 2, btnY, cx + bw / 2, btnY + 44);
        using var sb = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, Color = new SKColor(26, 26, 26, 80) };
        canvas.DrawRoundRect(_settingsRect, 22, 22, sb);
        string icon = vm.SoundEnabled ? "🔊" : "🔇";
        using var st = new SKPaint { IsAntialias = true, Color = new SKColor(26, 26, 26, 150), TextSize = 14, TextAlign = SKTextAlign.Center };
        canvas.DrawText($"{icon}  SES {(vm.SoundEnabled ? "AÇIK" : "KAPALI")}", cx, btnY + 27, st);

        // İstatistikler
        float sy = size.Height * 0.82f;
        using var vp = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1A1A1A"), TextSize = 20, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Helvetica", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) };
        using var lp2 = new SKPaint { IsAntialias = true, Color = new SKColor(26, 26, 26, 100), TextSize = 11, TextAlign = SKTextAlign.Center };

        float c1 = size.Width * 0.25f, c2 = size.Width * 0.5f, c3 = size.Width * 0.75f;
        canvas.DrawText(vm.HighScore.ToString(), c1, sy, vp);
        canvas.DrawText("EN İYİ SKOR", c1, sy + 18, lp2);
        canvas.DrawText(vm.BestLevel.ToString(), c2, sy, vp);
        canvas.DrawText("EN İYİ SEVİYE", c2, sy + 18, lp2);
        canvas.DrawText(vm.TotalGames.ToString(), c3, sy, vp);
        canvas.DrawText("TOPLAM OYUN", c3, sy + 18, lp2);

        // Alt yazı
        float pulse = 0.4f + 0.3f * MathF.Sin(time * 2f);
        using var fp = new SKPaint { IsAntialias = true, Color = new SKColor(26, 26, 26, (byte)(pulse * 255)), TextSize = 12, TextAlign = SKTextAlign.Center };
        canvas.DrawText("Odaklanmayı başarabilir misin?", cx, size.Height - 40, fp);
    }

    public MenuButton HitTest(float x, float y)
    {
        if (_playRect.Contains(x, y)) return MenuButton.Play;
        if (_continueRect.Contains(x, y)) return MenuButton.Continue;
        if (_settingsRect.Contains(x, y)) return MenuButton.Settings;
        return MenuButton.None;
    }
}
