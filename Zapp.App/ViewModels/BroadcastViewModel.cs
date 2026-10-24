using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.App.ViewModels;

// represent public alert card item
public sealed record BroadcastAlertItem(
    string SenderAlias,
    string Initials,
    string Timestamp,
    string MessageText,
    bool IsDirect,
    int HopsCount,
    int ReachablePeersCount,
    bool IsEmergency
);

// manage public broadcast channel and emergency transmission
public partial class BroadcastViewModel : BaseViewModel
{
    private readonly ITransportAdapter _transportAdapter;

    public ObservableCollection<BroadcastAlertItem> Alerts { get; } = [];

    [ObservableProperty]
    private string _publicMessageText = string.Empty;

    [ObservableProperty]
    private bool _markAsUrgent;

    [ObservableProperty]
    private int _connectedPeersCount = 12;

    [ObservableProperty]
    private int _activeRelaysCount = 3;

    public BroadcastViewModel(ITransportAdapter transportAdapter)
    {
        // bind transport abstraction
        _transportAdapter = transportAdapter;
        Title = "Alertes locales";
        LoadSampleAlerts();
    }

    // populate prototype broadcast alerts
    private void LoadSampleAlerts()
    {
        Alerts.Clear();
        Alerts.Add(new BroadcastAlertItem(
            "Metro_Node", "MN", "10:04",
            "La rue des Ormes est temporairement bloquée. Prenez l'itinéraire nord.",
            true, 0, 10, false));

        Alerts.Add(new BroadcastAlertItem(
            "Safety_Alpha", "SA", "10:08",
            "ALERTE : orage violent attendu dans 20 minutes. Mettez-vous à l'abri.",
            false, 2, 31, true));

        Alerts.Add(new BroadcastAlertItem(
            "Echo_Zero", "EZ", "10:11",
            "Point d'entraide ouvert au gymnase central.",
            false, 1, 22, false));
    }

    // publish broadcast alert to all neighboring nodes
    [RelayCommand]
    private async Task SendBroadcastAsync()
    {
        if (string.IsNullOrWhiteSpace(PublicMessageText))
        {
            // ignore empty input
            return;
        }

        await RunSafeAsync(async () =>
        {
            var packet = new NetworkPacket(
                PacketId: Guid.NewGuid().ToString(),
                Type: PacketType.Broadcast,
                SenderPublicKey: "local_node",
                RecipientPublicKey: "0000",
                Ttl: MarkAsUrgent ? 3 : 1,
                Payload: PublicMessageText,
                Signature: "sig_broadcast",
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );

            // transmit packet via broadcast transport
            await _transportAdapter.BroadcastAsync(packet);

            // prepend newly dispatched alert to collection
            Alerts.Insert(0, new BroadcastAlertItem(
                "Moi", "ME", "À l'instant",
                PublicMessageText,
                true, 0, ConnectedPeersCount, MarkAsUrgent));

            PublicMessageText = string.Empty;
            MarkAsUrgent = false;
        });
    }
}
