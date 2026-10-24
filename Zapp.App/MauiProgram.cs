using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace Zapp.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkit();
        builder.Services.AddTransient<Zapp.App.Views.Onboarding.PermissionsPage>();
        builder.Services.AddTransient<Zapp.App.Views.Radar.RadarPage>();
        builder.Services.AddTransient<Zapp.App.Views.Chats.ChatDetailPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}