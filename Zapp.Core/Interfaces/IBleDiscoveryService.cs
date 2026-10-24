using Zapp.Core.Common;
using Zapp.Core.Models;

namespace Zapp.Core.Interfaces;

// define contract for radio advertisement and neighbor discovery
public interface IBleDiscoveryService
{
    // check whether radio adapter is currently scanning
    bool IsScanning { get; }

    // expose stream of discovered neighbor advertisements
    event EventHandler<DiscoveredPeerAdvertisement>? PeerDiscovered;

    // begin broadcasting own node advertisement beacon
    Task<Result<bool>> StartAdvertisingAsync(string displayName, string publicKeyBase64, CancellationToken cancellationToken = default);

    // stop broadcasting own node beacon
    Task<Result<bool>> StopAdvertisingAsync(CancellationToken cancellationToken = default);

    // start scanning for neighboring peer beacons
    Task<Result<bool>> StartScanningAsync(CancellationToken cancellationToken = default);

    // stop active radio scan
    Task<Result<bool>> StopScanningAsync(CancellationToken cancellationToken = default);
}