using SQLite;
using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;
using Zapp.Infrastructure.Persistence.Entities;

namespace Zapp.Infrastructure.Persistence;

// provide sqlite implementation for offline storage and relay cache
public sealed class SqliteMessageRepository : IMessageRepository
{
    private readonly SQLiteAsyncConnection _database;

    public SqliteMessageRepository(string databasePath)
    {
        // initialize sqlite connection with full mutex
        _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    // initialize required sqlite schema tables
    public async Task InitializeAsync()
    {
        // create tables concurrently if missing
        await _database.CreateTableAsync<PeerEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<MessageEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<RelayCacheEntity>().ConfigureAwait(false);
    }

    // insert or update discovered peer record
    public async Task<Result<bool>> UpsertPeerAsync(PeerNode peer, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new PeerEntity
            {
                PublicKey = peer.PublicKey,
                DisplayName = peer.DisplayName,
                LastSeenTicks = peer.LastSeenTicks,
                Rssi = peer.Rssi,
                IsPaired = peer.IsPaired
            };

            // insert or replace peer record
            await _database.InsertOrReplaceAsync(entity).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // capture peer upsert error
            return Result<bool>.Failure(ex.Message);
        }
    }

    // retrieve all tracked peers ordered by latest activity
    public async Task<Result<IReadOnlyList<PeerNode>>> GetPeersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // query all peers ordered by last contact timestamp
            var entities = await _database.Table<PeerEntity>()
                .OrderByDescending(p => p.LastSeenTicks)
                .ToListAsync()
                .ConfigureAwait(false);

            var peers = entities.Select(e => new PeerNode(
                e.PublicKey,
                e.DisplayName,
                e.LastSeenTicks,
                e.Rssi,
                e.IsPaired
            )).ToList();

            return Result<IReadOnlyList<PeerNode>>.Success(peers);
        }
        catch (Exception ex)
        {
            // capture peer query error
            return Result<IReadOnlyList<PeerNode>>.Failure(ex.Message);
        }
    }

    // persist single message record into database
    public async Task<Result<bool>> SaveMessageAsync(LocalMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new MessageEntity
            {
                Id = message.Id,
                SenderKey = message.SenderKey,
                RecipientKey = message.RecipientKey,
                EncryptedContent = message.EncryptedContent,
                Timestamp = message.Timestamp,
                DeliveryStatus = message.DeliveryStatus.ToString(),
                HopsCount = message.HopsCount
            };

            // insert or update message entry
            await _database.InsertOrReplaceAsync(entity).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // capture message save error
            return Result<bool>.Failure(ex.Message);
        }
    }

    // fetch all pending messages queued in outbox
    public async Task<Result<IReadOnlyList<LocalMessage>>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingStatus = MessageDeliveryStatus.Pending.ToString();

            // filter records by pending status
            var entities = await _database.Table<MessageEntity>()
                .Where(m => m.DeliveryStatus == pendingStatus)
                .OrderBy(m => m.Timestamp)
                .ToListAsync()
                .ConfigureAwait(false);

            var messages = entities.Select(e => new LocalMessage(
                e.Id,
                e.SenderKey,
                e.RecipientKey,
                e.EncryptedContent,
                e.Timestamp,
                Enum.Parse<MessageDeliveryStatus>(e.DeliveryStatus),
                e.HopsCount
            )).ToList();

            return Result<IReadOnlyList<LocalMessage>>.Success(messages);
        }
        catch (Exception ex)
        {
            // capture outbox retrieval error
            return Result<IReadOnlyList<LocalMessage>>.Failure(ex.Message);
        }
    }

    // update message delivery lifecycle state
    public async Task<Result<bool>> UpdateMessageStatusAsync(string messageId, MessageDeliveryStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            // find target message by id
            var message = await _database.Table<MessageEntity>()
                .Where(m => m.Id == messageId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (message is null)
            {
                // return failure when record is missing
                return Result<bool>.Failure($"message {messageId} not found");
            }

            // update status string representation
            message.DeliveryStatus = status.ToString();
            await _database.UpdateAsync(message).ConfigureAwait(false);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // capture status update error
            return Result<bool>.Failure(ex.Message);
        }
    }

    // check if packet id exists in relay cache
    public async Task<Result<bool>> HasRelayedPacketAsync(string packetId, CancellationToken cancellationToken = default)
    {
        try
        {
            // count matching entries in relay table
            var count = await _database.Table<RelayCacheEntity>()
                .Where(r => r.PacketId == packetId)
                .CountAsync()
                .ConfigureAwait(false);

            return Result<bool>.Success(count > 0);
        }
        catch (Exception ex)
        {
            // capture cache check error
            return Result<bool>.Failure(ex.Message);
        }
    }

    // record packet into deduplication cache
    public async Task<Result<bool>> RecordRelayedPacketAsync(string packetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new RelayCacheEntity
            {
                PacketId = packetId,
                RelayedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // insert entry to prevent re-forwarding
            await _database.InsertOrReplaceAsync(entity).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // capture relay cache save error
            return Result<bool>.Failure(ex.Message);
        }
    }
}