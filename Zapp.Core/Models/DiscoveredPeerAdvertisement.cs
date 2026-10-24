namespace Zapp.Core.Models;

// represent raw radio discovery payload emitted by nearby peer
public sealed record DiscoveredPeerAdvertisement(
    string DeviceAddress,
    string ServiceUuid,
    string PublicKeyBase64,
    string DisplayName,
    int Rssi,
    long ObservedAtTicks
);