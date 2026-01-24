using DailyJournal.Core.Services;
using DailyJournal.Data.Services;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SabinDevkota.Data.Services;

namespace DailyJournal
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            SQLitePCL.Batteries_V2.Init();
            builder.Services.AddMudServices();


            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IJournalService, JournalService>();
            builder.Services.AddSingleton<IDashboardService, DashboardService>();
            builder.Services.AddSingleton<IPdfService, Pdfservices>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
