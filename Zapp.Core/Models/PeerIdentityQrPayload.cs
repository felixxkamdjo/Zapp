namespace Zapp.Core.Models;

// encapsulate compressed identity schema for offline optical exchange
public sealed record PeerIdentityQrPayload(
    string PublicKeyBase64,
    string DisplayName,
    long IssuedAt
);
