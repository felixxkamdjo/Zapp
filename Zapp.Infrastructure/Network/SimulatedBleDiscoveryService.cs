using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.Infrastructure.Network;

// simulate ble discovery for testing mesh propagation without physical radios
public sealed class SimulatedBleDiscoveryService : IBleDiscoveryService
{
    private readonly object _lock = new();
    public bool IsScanning { get; private set; }
    public bool IsAdvertising { get; private set; }

    public event EventHandler<DiscoveredPeerAdvertisement>? PeerDiscovered;

    // trigger simulated beacon reception for unit tests and local mesh
    public void InjectSimulatedPeer(DiscoveredPeerAdvertisement advertisement)
    {
        lock (_lock)
        {
            if (IsScanning)
            {
                // dispatch event to active subscribers
                PeerDiscovered?.Invoke(this, advertisement);
            }
        }
    }

    // start broadcasting simulated beacon
    public Task<Result<bool>> StartAdvertisingAsync(string displayName, string publicKeyBase64, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // toggle advertising state
            IsAdvertising = true;
            return Task.FromResult(Result<bool>.Success(true));
        }
    }

    // stop broadcasting simulated beacon
    public Task<Result<bool>> StopAdvertisingAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // disable advertising state
            IsAdvertising = false;
            return Task.FromResult(Result<bool>.Success(true));
        }
    }

    // start simulated radio scanning
    public Task<Result<bool>> StartScanningAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // toggle scanning state
            IsScanning = true;
            return Task.FromResult(Result<bool>.Success(true));
        }
    }

    // stop simulated radio scanning
    public Task<Result<bool>> StopScanningAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // disable scanning state
            IsScanning = false;
            return Task.FromResult(Result<bool>.Success(true));
        }
    }
}