namespace Zapp.Core.Models;

// represent ed25519 public and private key pair
public sealed record KeyBundle(
    string PublicKeyBase64,
    string PrivateKeyBase64,
    long CreatedAt
);