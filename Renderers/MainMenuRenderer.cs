using AAPlus.ViewModels;
using SkiaSharp;

namespace AAPlus.Renderers;

public enum MenuButton { None, Play, Continue, Settings }

/// <summary>
/// Ana menü — siyah arka plan, beyaz minimalist tasarım.
/// </summary>
public class MainMenuRenderer
{
    private SKRect _playRect, _continueRect, _settingsRect;
    private readonly float[] _orbits = { 0, 1.05f, 2.1f, 3.15f, 4.2f, 5.25f };

    private static readonly SKColor Bg = SKColor.Parse("#111111");
    private static readonly SKColor White = SKColors.White;
    private static readonly SKColor Dim = new(255, 255, 255, 100);
    private static readonly SKColor CircleBg = SKColor.Parse("#222222");

    public void Draw(SKCanvas c, SKSize sz, MainMenuViewModel vm, float time)
    {
        c.Clear(Bg);
        float cx = sz.Width / 2f;
        float circleY = sz.Height * 0.30f;

        // Dekoratif dönen iğneler
        for (int i = 0; i < 6; i++)
        {
            float a = _orbits[i] + time * (0.4f + i * 0.06f);
            float sin = MathF.Sin(a), cos = MathF.Cos(a);
            float x1 = cx + sin * 50f, y1 = circleY - cos * 50f;
            float x2 = cx + sin * 110f, y2 = circleY - cos * 110f;

            using var lp = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f, Color = new SKColor(255, 255, 255, 30), StrokeCap = SKStrokeCap.Round
            };
            c.DrawLine(x1, y1, x2, y2, lp);

            using var dp = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Fill,
                Color = new SKColor(255, 255, 255, 50)
            };
            c.DrawCircle(x2, y2, 6f, dp);
        }

        // Merkez daire
        using var cf = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = CircleBg };
        c.DrawCircle(cx, circleY, 50f, cf);
        using var cs = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f, Color = new SKColor(255, 255, 255, 30)
        };
        c.DrawCircle(cx, circleY, 50f, cs);

        // "aa" logosu
        using var logo = new SKPaint
        {
            IsAntialias = true, Color = White, TextSize = 32,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.Thin, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("aa", cx, circleY + 11, logo);

        // Best level alt yazı
        using var bl = new SKPaint
        {
            IsAntialias = true, Color = Dim, TextSize = 10,
            TextAlign = SKTextAlign.Center
        };
        c.DrawText($"BEST LEVEL: {vm.BestLevel}", cx, circleY + 30, bl);

        // ═══ BUTONLAR ═══
        float bw = Math.Min(200, sz.Width * 0.5f);
        float btnY = sz.Height * 0.54f;

        // OYNA
        _playRect = new SKRect(cx - bw / 2, btnY, cx + bw / 2, btnY + 48);
        using var pbg = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = White };
        c.DrawRoundRect(_playRect, 24, 24, pbg);
        using var ptx = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#111111"), TextSize = 16,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("▶  OYNA", cx, btnY + 30, ptx);

        btnY += 60;
        if (vm.CanContinue)
        {
            _continueRect = new SKRect(cx - bw / 2, btnY, cx + bw / 2, btnY + 44);
            using var cb = new SKPaint
            {
                IsAntialias = true, Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f, Color = White
            };
            c.DrawRoundRect(_continueRect, 22, 22, cb);
            using var ct = new SKPaint
            {
                IsAntialias = true, Color = White, TextSize = 14,
                TextAlign = SKTextAlign.Center
            };
            string info = vm.SavedPinsRemaining > 0
                ? $"Level {vm.SavedLevel}  ·  {vm.SavedPinsRemaining} iğne kaldı"
                : $"Level {vm.SavedLevel}";
            c.DrawText($"DEVAM ET  ›  {info}", cx, btnY + 28, ct);
            btnY += 56;
        }

        // Ses
        _settingsRect = new SKRect(cx - bw / 2, btnY, cx + bw / 2, btnY + 40);
        using var sb = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f, Color = new SKColor(255, 255, 255, 50)
        };
        c.DrawRoundRect(_settingsRect, 20, 20, sb);
        string icon = vm.SoundEnabled ? "🔊" : "🔇";
        using var st = new SKPaint
        {
            IsAntialias = true, Color = Dim, TextSize = 13,
            TextAlign = SKTextAlign.Center
        };
        c.DrawText($"{icon}  SES {(vm.SoundEnabled ? "AÇIK" : "KAPALI")}", cx, btnY + 25, st);

        // ═══ İSTATİSTİKLER ═══
        float sy = sz.Height * 0.84f;
        using var vp = new SKPaint
        {
            IsAntialias = true, Color = White, TextSize = 18,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Helvetica Neue",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        using var lbl = new SKPaint
        {
            IsAntialias = true, Color = Dim, TextSize = 10,
            TextAlign = SKTextAlign.Center
        };

        float c1 = sz.Width * 0.25f, c2 = sz.Width * 0.5f, c3 = sz.Width * 0.75f;
        c.DrawText(vm.HighScore.ToString(), c1, sy, vp);
        c.DrawText("BEST SCORE", c1, sy + 16, lbl);
        c.DrawText(vm.BestLevel.ToString(), c2, sy, vp);
        c.DrawText("BEST LEVEL", c2, sy + 16, lbl);
        c.DrawText(vm.TotalGames.ToString(), c3, sy, vp);
        c.DrawText("TOTAL GAMES", c3, sy + 16, lbl);

        // Alt yazı
        float pulse = 0.3f + 0.3f * MathF.Sin(time * 2f);
        using var fp = new SKPaint
        {
            IsAntialias = true, Color = new SKColor(255, 255, 255, (byte)(pulse * 255)),
            TextSize = 12, TextAlign = SKTextAlign.Center
        };
        c.DrawText("Odaklanmayı başarabilir misin?", cx, sz.Height - 35, fp);
    }

    public MenuButton HitTest(float x, float y)
    {
        if (_playRect.Contains(x, y)) return MenuButton.Play;
        if (_continueRect.Contains(x, y)) return MenuButton.Continue;
        if (_settingsRect.Contains(x, y)) return MenuButton.Settings;
        return MenuButton.None;
    }
}