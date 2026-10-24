namespace Zapp.Core.Models;

// define supported network packet categories
public enum PacketType
{
    Direct,
    Relay,
    Broadcast,
    Beacon
}

// encapsulate immutable wire network frame
public sealed record NetworkPacket(
    string PacketId,
    PacketType Type,
    string SenderPublicKey,
    string RecipientPublicKey,
    int Ttl,
    string Payload,
    string Signature,
    long Timestamp
)
{
    // decrement time-to-live counter during mesh relay
    public NetworkPacket DecrementTtl() => this with { Ttl = Ttl - 1 };

    // verify remaining hops availability
    public bool IsRoutable => Ttl > 0;
}