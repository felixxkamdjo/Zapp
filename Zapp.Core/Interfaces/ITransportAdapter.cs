using Zapp.Core.Common;
using Zapp.Core.Models;

namespace Zapp.Core.Interfaces;

// define contract for physical and simulated packet delivery mediums
public interface ITransportAdapter
{
    // expose transport protocol identifier
    string MediumName { get; }

    // determine if medium is currently capable of transmission
    bool IsAvailable { get; }

    // send packet directly to a reachable peer
    Task<Result<bool>> SendDirectAsync(string targetAddress, NetworkPacket packet, CancellationToken cancellationToken = default);

    // broadcast packet to all reachable neighboring peers
    Task<Result<int>> BroadcastAsync(NetworkPacket packet, CancellationToken cancellationToken = default);

    // stream incoming packets received on this medium
    event EventHandler<NetworkPacket>? PacketReceived;
}