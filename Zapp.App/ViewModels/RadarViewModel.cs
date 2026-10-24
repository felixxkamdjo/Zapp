using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.App.ViewModels;

// manage peer discovery radar state and scan operations
public partial class RadarViewModel : BaseViewModel
{
    private readonly IBleDiscoveryService _discoveryService;
    private readonly IMessageRepository _repository;

    public ObservableCollection<PeerNode> DiscoveredPeers { get; } = [];

    [ObservableProperty]
    private bool _isScanning;

    public RadarViewModel(IBleDiscoveryService discoveryService, IMessageRepository repository)
    {
        // bind service abstractions
        _discoveryService = discoveryService;
        _repository = repository;
        Title = "Radar";

        // register listener for neighbor beacons
        _discoveryService.PeerDiscovered += OnPeerDiscovered;
    }

    // toggle radio discovery scan
    [RelayCommand]
    private async Task ToggleScanAsync()
    {
        await RunSafeAsync(async () =>
        {
            if (IsScanning)
            {
                // halt active scanner
                await _discoveryService.StopScanningAsync().ConfigureAwait(false);
                IsScanning = false;
            }
            else
            {
                // trigger neighbor discovery
                await _discoveryService.StartScanningAsync().ConfigureAwait(false);
                IsScanning = true;
            }
        });
    }

    // process incoming neighbor beacon and update observable list
    private void OnPeerDiscovered(object? sender, DiscoveredPeerAdvertisement advert)
    {
        // execute observable update on main ui thread
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var existing = DiscoveredPeers.FirstOrDefault(p => p.PublicKey == advert.PublicKeyBase64);
            var updatedNode = new PeerNode(
                PublicKey: advert.PublicKeyBase64,
                DisplayName: advert.DisplayName,
                LastSeenTicks: advert.ObservedAtTicks,
                Rssi: advert.Rssi,
                IsPaired: existing?.IsPaired ?? false
            );

            if (existing is not null)
            {
                // replace existing peer item in collection
                var index = DiscoveredPeers.IndexOf(existing);
                DiscoveredPeers[index] = updatedNode;
            }
            else
            {
                // append newly detected neighbor node
                DiscoveredPeers.Add(updatedNode);
            }

            // persist node in local sqlite database
            await _repository.UpsertPeerAsync(updatedNode).ConfigureAwait(false);
        });
    }
}
