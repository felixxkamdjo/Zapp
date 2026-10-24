using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.Infrastructure.Network;

// simulate point-to-point and broadcast transmission in memory
public sealed class SimulatedTransportAdapter : ITransportAdapter
{
    private readonly object _lock = new();
    private readonly List<SimulatedTransportAdapter> _connectedNodes = [];

    public string MediumName => "Simulated-Mesh-Radio";
    public bool IsAvailable { get; set; } = true;

    public event EventHandler<NetworkPacket>? PacketReceived;

    // link adapter to virtual neighbor node
    public void ConnectToNode(SimulatedTransportAdapter neighbor)
    {
        lock (_lock)
        {
            if (!_connectedNodes.Contains(neighbor))
            {
                // register bidirectional virtual radio link
                _connectedNodes.Add(neighbor);
                neighbor._connectedNodes.Add(this);
            }
        }
    }

    // deliver packet directly to target peer
    public Task<Result<bool>> SendDirectAsync(string targetAddress, NetworkPacket packet, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!IsAvailable)
            {
                // abort when radio medium is disabled
                return Task.FromResult(Result<bool>.Failure("transport adapter offline"));
            }

            // dispatch packet to connected neighbor nodes
            foreach (var node in _connectedNodes)
            {
                node.ReceiveInboundPacket(packet);
            }

            return Task.FromResult(Result<bool>.Success(true));
        }
    }

    // broadcast packet to all connected neighboring nodes
    public Task<Result<int>> BroadcastAsync(NetworkPacket packet, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!IsAvailable)
            {
                // abort when radio medium is disabled
                return Task.FromResult(Result<int>.Failure("transport adapter offline"));
            }

            var deliveredCount = 0;

            // flood packet to all reachable neighbor nodes
            foreach (var node in _connectedNodes)
            {
                node.ReceiveInboundPacket(packet);
                deliveredCount++;
            }

            return Task.FromResult(Result<int>.Success(deliveredCount));
        }
    }

    // accept inbound frame and trigger event handler
    internal void ReceiveInboundPacket(NetworkPacket packet)
    {
        // fire event for registered listeners
        PacketReceived?.Invoke(this, packet);
    }
}