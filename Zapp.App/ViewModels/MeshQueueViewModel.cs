using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zapp.Core.Common;
using Zapp.Core.Interfaces;

namespace Zapp.App.ViewModels;

// encapsulate pending transit packet display model
public sealed record TransitPacketItem(
    string DestinationHash,
    string NextHopAlias,
    int HopsCount,
    string RemainingTime
);

// manage mesh queue metrics, storage gauge, and opportunistic relay toggle
public partial class MeshQueueViewModel : BaseViewModel
{
    private readonly IMessageRepository _repository;

    [ObservableProperty]
    private double _allocatedMb = 18.4;

    [ObservableProperty]
    private double _maxMb = 48.0;

    [ObservableProperty]
    private int _relayedPacketsCount = 246;

    [ObservableProperty]
    private int _expiredPacketsCount = 12;

    [ObservableProperty]
    private bool _isOpportunisticRelayEnabled = true;

    public ObservableCollection<TransitPacketItem> TransitPackets { get; } = [];

    public MeshQueueViewModel(IMessageRepository repository)
    {
        // bind message repository abstraction
        _repository = repository;
        Title = "File & relais Mesh";
        LoadSamplePackets();
    }

    // populate initial transit packets matching visual prototype
    private void LoadSamplePackets()
    {
        TransitPackets.Clear();
        TransitPackets.Add(new TransitPacketItem("...7A91", "Alice_Crypt", 2, "08:42"));
        TransitPackets.Add(new TransitPacketItem("...0CE4", "Nomad_42", 1, "17:16"));
        TransitPackets.Add(new TransitPacketItem("...88F2", "Echo_Zero", 3, "28:03"));
    }

    // purge local relay deduplication cache and reset counters
    [RelayCommand]
    private async Task ClearRelayCacheAsync()
    {
        await RunSafeAsync(async () =>
        {
            // simulate asynchronous cache purge operation
            await Task.Delay(150);
            TransitPackets.Clear();
            RelayedPacketsCount = 0;
            AllocatedMb = 0.0;
        });
    }
}
