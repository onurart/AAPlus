using AAPlus.Renderers;
using AAPlus.Services;
using AAPlus.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;

namespace AAPlus.Views;

public partial class PinGamePage : ContentPage
{
    private readonly PinGameViewModel _vm;
    private readonly PinGameRenderer _renderer = new();
    private readonly Stopwatch _sw = new();
    private float _lastTime;
    private IDispatcherTimer? _timer;
    private SKSize _lastSize;

    public PinGamePage(PinGameViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _vm.OnLevelTransition += () => _renderer.ResetLevelTimer();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Reset();
        _vm.Initialize();
        _sw.Restart();
        _lastTime = 0;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        // Sayfadan çıkarken kaydet
        _vm.AutoSave();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        float t = (float)_sw.Elapsed.TotalSeconds;
        float dt = t - _lastTime;
        _lastTime = t;
        _vm.UpdateFrame(dt);
        GameCanvas.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float scale = (float)DeviceDisplay.MainDisplayInfo.Density;
        canvas.Scale(scale);
        _lastSize = new SKSize(e.Info.Width / scale, e.Info.Height / scale);

        float t = (float)_sw.Elapsed.TotalSeconds;
        float dt = t - _lastTime;
        _renderer.Draw(canvas, _lastSize, _vm.Engine, dt);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Pressed) return;
        e.Handled = true;

        float scale = (float)DeviceDisplay.MainDisplayInfo.Density;
        float x = e.Location.X / scale;
        float y = e.Location.Y / scale;

        // Pause butonu
        if (_vm.Engine.State == GameState.Playing && _renderer.IsPauseHit(x, y, _lastSize))
        {
            _vm.PauseTapped();
            return;
        }

        _vm.TapCommand.Execute(null);
    }
}
