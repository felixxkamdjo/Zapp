using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.Core.Services;

// evaluate incoming mesh packets for local consumption or multi-hop forwarding
public sealed class MeshRouter
{
    private readonly string _localPublicKey;
    private readonly IMessageRepository _repository;
    private readonly ITransportAdapter _transportAdapter;

    public MeshRouter(string localPublicKey, IMessageRepository repository, ITransportAdapter transportAdapter)
    {
        // bind identity and dependencies
        _localPublicKey = localPublicKey;
        _repository = repository;
        _transportAdapter = transportAdapter;
    }

    // handle inbound packet routing decision
    public async Task<Result<bool>> ProcessInboundPacketAsync(NetworkPacket packet, CancellationToken cancellationToken = default)
    {
        // drop packet if already processed previously to prevent endless loops
        var duplicateCheck = await _repository.HasRelayedPacketAsync(packet.PacketId, cancellationToken);
        if (duplicateCheck.IsSuccess && duplicateCheck.Value)
        {
            // discard duplicate frame silently
            return Result<bool>.Success(false);
        }

        // record packet id into relay cache
        await _repository.RecordRelayedPacketAsync(packet.PacketId, cancellationToken);

        // deliver locally if targeted to this node or broadcast
        if (packet.RecipientPublicKey == _localPublicKey || packet.Type == PacketType.Broadcast)
        {
            // store message locally as delivered
            var localMessage = new LocalMessage(
                Id: packet.PacketId,
                SenderKey: packet.SenderPublicKey,
                RecipientKey: packet.RecipientPublicKey,
                EncryptedContent: packet.Payload,
                Timestamp: packet.Timestamp,
                DeliveryStatus: MessageDeliveryStatus.Delivered,
                HopsCount: 3 - packet.Ttl
            );

            await _repository.SaveMessageAsync(localMessage, cancellationToken);
            return Result<bool>.Success(true);
        }

        // evaluate forwarding capability via ttl check
        if (!packet.IsRoutable)
        {
            // drop exhausted packet
            return Result<bool>.Success(false);
        }

        // decrement ttl and mutate packet type to relay
        var forwardedPacket = packet.DecrementTtl() with { Type = PacketType.Relay };

        // store copy in outbox with transit status
        var transitMessage = new LocalMessage(
            Id: forwardedPacket.PacketId,
            SenderKey: forwardedPacket.SenderPublicKey,
            RecipientKey: forwardedPacket.RecipientPublicKey,
            EncryptedContent: forwardedPacket.Payload,
            Timestamp: forwardedPacket.Timestamp,
            DeliveryStatus: MessageDeliveryStatus.RelayedInTransit,
            HopsCount: 3 - forwardedPacket.Ttl
        );

        await _repository.SaveMessageAsync(transitMessage, cancellationToken);

        // flood forwarded packet to adjacent nodes
        await _transportAdapter.BroadcastAsync(forwardedPacket, cancellationToken);

        return Result<bool>.Success(true);
    }
}