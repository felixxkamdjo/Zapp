namespace Zapp.Core.Models;

// represent discovered or paired mesh network node
public sealed record PeerNode(
    string PublicKey,
    string DisplayName,
    long LastSeenTicks,
    int Rssi,
    bool IsPaired
);