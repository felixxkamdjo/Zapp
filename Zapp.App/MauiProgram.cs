using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Zapp.App.ViewModels;
using Zapp.App.Views.Broadcast;
using Zapp.App.Views.Chats;
using Zapp.App.Views.Mesh;
using Zapp.App.Views.Onboarding;
using Zapp.App.Views.Radar;
using Zapp.App.Views.Settings;
using Zapp.Core.Interfaces;
using Zapp.Core.Services;
using Zapp.Infrastructure.Cryptography;
using Zapp.Infrastructure.Network;
using Zapp.Infrastructure.Persistence;
using ZXing.Net.Maui.Controls;

namespace Zapp.App;

// configure maui application host and register dependencies
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // resolve database storage path inside sandboxed storage
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "zapp_v1.db");

        // register core and infrastructure singletons
        builder.Services.AddSingleton<IMessageRepository>(_ =>
        {
            var repo = new SqliteMessageRepository(dbPath);
            repo.InitializeAsync().GetAwaiter().GetResult();
            return repo;
        });

        builder.Services.AddSingleton<ICryptoService, NSecCryptoService>();
        builder.Services.AddSingleton<IPacketSerializer, SystemJsonPacketSerializer>();
        builder.Services.AddSingleton<IBleDiscoveryService, SimulatedBleDiscoveryService>();
        builder.Services.AddSingleton<ITransportAdapter, SimulatedTransportAdapter>();

        // register transient viewmodels
        builder.Services.AddTransient<RadarViewModel>();
        builder.Services.AddTransient<MeshQueueViewModel>();
        builder.Services.AddTransient<BroadcastViewModel>();
        builder.Services.AddTransient<FaqViewModel>();
        builder.Services.AddTransient<IdentityExchangeViewModel>(sp =>
        {
            var repo = sp.GetRequiredService<IMessageRepository>();
            var crypto = sp.GetRequiredService<ICryptoService>();
            var keys = crypto.GenerateKeyPair().Value!;
            return new IdentityExchangeViewModel(repo, keys.PublicKeyBase64, "Mon_Terminal_Zapp");
        });

        builder.Services.AddTransient<ChatsViewModel>(sp =>
        {
            var repo = sp.GetRequiredService<IMessageRepository>();
            var crypto = sp.GetRequiredService<ICryptoService>();
            var transport = sp.GetRequiredService<ITransportAdapter>();

            var keys = crypto.GenerateKeyPair().Value!;
            return new ChatsViewModel(repo, crypto, transport, keys.PrivateKeyBase64, keys.PublicKeyBase64);
        });

        // register view pages
        builder.Services.AddTransient<PermissionsPage>();
        builder.Services.AddTransient<MeshIntroPage>();
        builder.Services.AddTransient<RadarPage>();
        builder.Services.AddTransient<ChatDetailPage>();
        builder.Services.AddTransient<MeshQueuePage>();
        builder.Services.AddTransient<BroadcastAlertsPage>();
        builder.Services.AddTransient<FaqAssistantPage>();
        builder.Services.AddTransient<IdentityExchangePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
