using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using AAPlus.Services;
using AAPlus.ViewModels;
using AAPlus.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace AAPlus;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services — Singleton (tek instance, tüm sayfalar paylaşır)
        builder.Services.AddSingleton<SaveManager>();
        builder.Services.AddSingleton<AudioHapticService>();
        builder.Services.AddSingleton<GameDataService>();

        // ViewModels — Transient (her sayfa açılışında yeni)
        builder.Services.AddTransient<MainMenuViewModel>();
        builder.Services.AddTransient<PinGameViewModel>();

        // Pages
        builder.Services.AddTransient<MainMenuPage>();
        builder.Services.AddTransient<PinGamePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
