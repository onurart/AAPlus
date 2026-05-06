using AAPlus.Services;
using AAPlus.ViewModels;

namespace AAPlus;

public partial class App : Application
{
    private readonly SaveManager _save;

    public App(SaveManager save)
    {
        InitializeComponent();
        _save = save;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // ─── Lifecycle Events ───────────────────────────────
        window.Deactivated += (_, _) => HandleSleep();
        window.Stopped += (_, _) => HandleSleep();
        window.Destroying += (_, _) => HandleSleep();
        window.Resumed += async (_, _) => await OnResumeAsync();

        // İlk açılışta kayıt yükle
        _ = InitializeAsync();

        return window;
    }

    private async Task InitializeAsync()
    {
        await _save.LoadAsync();
        System.Diagnostics.Debug.WriteLine(
            $"[App] Kayıt yüklendi: Level {_save.Data.CurrentLevel}, " +
            $"HighScore {_save.Data.HighScore}, " +
            $"HasActiveGame {_save.Data.HasActiveGame}");
    }

    /// <summary>Uygulama arka plana alındığında veya kapanırken.</summary>
    private void HandleSleep()
    {
        System.Diagnostics.Debug.WriteLine("[App] Sleep — otomatik kayıt yapılıyor...");

        // Aktif oyun varsa state'i yakala
        if (Shell.Current?.CurrentPage?.BindingContext is PinGameViewModel vm)
        {
            vm.AutoSave();
        }

        // Senkron kaydet (async bazen kapanmadan tamamlanmayabilir)
        _save.SaveSync();
    }

    /// <summary>Uygulama geri döndüğünde.</summary>
    private async Task OnResumeAsync()
    {
        System.Diagnostics.Debug.WriteLine("[App] Resume");
        await _save.LoadAsync();
    }
}