using AAPlus.Renderers;
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

    public PinGamePage(PinGameViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _vm.OnLevelTransition += () => _renderer.ResetLevelCompleteTimer();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.ResetForNewGame();
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
        var size = new SKSize(e.Info.Width / scale, e.Info.Height / scale);
        float t = (float)_sw.Elapsed.TotalSeconds;
        _renderer.Draw(canvas, size, _vm.Engine, t - _lastTime);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            _vm.TapCommand.Execute(null);
            e.Handled = true;
        }
    }
}
