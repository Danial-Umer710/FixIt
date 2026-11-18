using FixIt.Services;
using FixIt.ViewModels;
using FixIt.Views;
using Microsoft.Extensions.Logging;

namespace FixIt;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // MAUI Essentials (device features) – injected so ViewModels/services stay testable
        builder.Services.AddSingleton(Connectivity.Current);
        builder.Services.AddSingleton(Geolocation.Default);
        builder.Services.AddSingleton(Geocoding.Default);
        builder.Services.AddSingleton(MediaPicker.Default);
        builder.Services.AddSingleton(Share.Default);
        builder.Services.AddSingleton(Map.Default);

        // App services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<PhotoService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<AddressSyncService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

        // Shell + tab pages live for the whole app lifetime
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<ReportsViewModel>();
        builder.Services.AddSingleton<ReportsPage>();
        builder.Services.AddSingleton<MapViewModel>();
        builder.Services.AddSingleton<MapPage>();

        // Pushed pages get a fresh instance each time
        builder.Services.AddTransient<ReportEditViewModel>();
        builder.Services.AddTransient<ReportEditPage>();
        builder.Services.AddTransient<ReportDetailViewModel>();
        builder.Services.AddTransient<ReportDetailPage>();

        return builder.Build();
    }
}
