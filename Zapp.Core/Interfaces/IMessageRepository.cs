using Zapp.Core.Common;
using Zapp.Core.Models;

namespace Zapp.Core.Interfaces;

// define persistence contract for offline messaging store
public interface IMessageRepository
{
    // save or update peer node metadata
    Task<Result<bool>> UpsertPeerAsync(PeerNode peer, CancellationToken cancellationToken = default);

    // retrieve list of known peers ordered by last seen
    Task<Result<IReadOnlyList<PeerNode>>> GetPeersAsync(CancellationToken cancellationToken = default);

    // store outgoing or incoming message record
    Task<Result<bool>> SaveMessageAsync(LocalMessage message, CancellationToken cancellationToken = default);

    // fetch all pending messages queued for direct or relay delivery
    Task<Result<IReadOnlyList<LocalMessage>>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default);

    // update delivery status for target message identifier
    Task<Result<bool>> UpdateMessageStatusAsync(string messageId, MessageDeliveryStatus status, CancellationToken cancellationToken = default);

    // verify if relayed packet has already been processed to prevent broadcast loops
    Task<Result<bool>> HasRelayedPacketAsync(string packetId, CancellationToken cancellationToken = default);

    // record forwarded packet into dedup cache
    Task<Result<bool>> RecordRelayedPacketAsync(string packetId, CancellationToken cancellationToken = default);
}