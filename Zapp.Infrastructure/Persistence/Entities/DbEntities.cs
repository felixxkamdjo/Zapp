using SQLite;

namespace Zapp.Infrastructure.Persistence.Entities;

// map peers schema for sqlite storage
[Table("Peers")]
public sealed class PeerEntity
{
    [PrimaryKey]
    public string PublicKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public long LastSeenTicks { get; set; }

    public int Rssi { get; set; }

    public bool IsPaired { get; set; }
}

// map local message records for sqlite storage
[Table("Messages")]
public sealed class MessageEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string SenderKey { get; set; } = string.Empty;

    [Indexed]
    public string RecipientKey { get; set; } = string.Empty;

    public string EncryptedContent { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    public string DeliveryStatus { get; set; } = string.Empty;

    public int HopsCount { get; set; }
}

// map relay deduplication cache table
[Table("RelayCache")]
public sealed class RelayCacheEntity
{
    [PrimaryKey]
    public string PacketId { get; set; } = string.Empty;

    public long RelayedAt { get; set; }
}