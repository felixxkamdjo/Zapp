using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.Core.Services;

// select suitable transport adapter based on packet delivery target
public sealed class TransportStrategySelector
{
    private readonly IEnumerable<ITransportAdapter> _adapters;

    public TransportStrategySelector(IEnumerable<ITransportAdapter> adapters)
    {
        // initialize collection of registered transport adapters
        _adapters = adapters;
    }

    // route packet via appropriate medium strategy
    public async Task<Result<bool>> RoutePacketAsync(NetworkPacket packet, string? targetAddress = null, CancellationToken cancellationToken = default)
    {
        // resolve first active adapter available
        var adapter = _adapters.FirstOrDefault(a => a.IsAvailable);
        if (adapter is null)
        {
            // reject transmission when no medium is active
            return Result<bool>.Failure("no active transport adapter found");
        }

        // switch routing mechanism based on packet type
        return packet.Type switch
        {
            PacketType.Broadcast or PacketType.Beacon => await adapter.BroadcastAsync(packet, cancellationToken).ContinueWith(t =>
                t.Result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(t.Result.ErrorMessage!), cancellationToken),

            PacketType.Direct or PacketType.Relay when !string.IsNullOrWhiteSpace(targetAddress) =>
                await adapter.SendDirectAsync(targetAddress, packet, cancellationToken),

            _ => Result<bool>.Failure("unsupported packet routing parameters")
        };
    }
}