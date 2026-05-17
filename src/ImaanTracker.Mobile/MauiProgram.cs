using CommunityToolkit.Maui;
using ImaanTracker.Mobile.Services;
using ImaanTracker.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace ImaanTracker.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<SignUpPage>();
        builder.Services.AddTransient<PrayerChecklistPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
