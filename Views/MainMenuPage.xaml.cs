using AAPlus.Renderers;
using AAPlus.Services;
using AAPlus.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;

namespace AAPlus.Views;

public partial class MainMenuPage : ContentPage
{
    private readonly MainMenuViewModel _vm;
    private readonly SaveManager _save;
    private readonly MainMenuRenderer _renderer = new();
    private readonly Stopwatch _sw = new();
    private IDispatcherTimer? _timer;

    public MainMenuPage(MainMenuViewModel vm, SaveManager save)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _save = save;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadData();
        _sw.Restart();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, _) => MenuCanvas.InvalidateSurface();
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float scale = (float)DeviceDisplay.MainDisplayInfo.Density;
        canvas.Scale(scale);
        var size = new SKSize(e.Info.Width / scale, e.Info.Height / scale);
        _renderer.Draw(canvas, size, _vm, (float)_sw.Elapsed.TotalSeconds);
    }

    private async void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType != SKTouchAction.Pressed) return;
        e.Handled = true;

        float scale = (float)DeviceDisplay.MainDisplayInfo.Density;
        float x = e.Location.X / scale, y = e.Location.Y / scale;

        switch (_renderer.HitTest(x, y))
        {
            case MenuButton.Play:
                // Yeni oyun — flag ÖNCE set et
                _save.ContinueRequested = false;
                _save.ClearActiveGame();
                await Shell.Current.GoToAsync("//PinGamePage");
                break;

            case MenuButton.Continue:
                // DEVAM ET — flag ÖNCE set et, sonra navigate
                _save.ContinueRequested = true;
                await Shell.Current.GoToAsync("//PinGamePage");
                break;

            case MenuButton.Settings:
                _vm.ToggleSound();
                break;
        }
    }
}
