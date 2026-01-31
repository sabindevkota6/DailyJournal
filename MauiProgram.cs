using DailyJournal.Core.Services;
using DailyJournal.Data.Services;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SabinDevkota.Data.Services;

namespace DailyJournal
{
    // entry point for the MAUI application configuration
    public static class MauiProgram
    {
        // creating and configuring the MAUI application
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // initializing SQLite for database operations
            SQLitePCL.Batteries_V2.Init();

            // adding MudBlazor UI component services
            builder.Services.AddMudServices();

            // adding Blazor WebView for hybrid app support
            builder.Services.AddMauiBlazorWebView();

            // registering application services as singletons
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IJournalService, JournalService>();
            builder.Services.AddSingleton<IDashboardService, DashboardService>();
            builder.Services.AddSingleton<IPdfService, Pdfservices>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();

#if DEBUG
            // enabling developer tools and debug logging in debug mode
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
