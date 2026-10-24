using Zapp.Core.Common;
using Zapp.Core.Models;

namespace Zapp.Core.Interfaces;

// define operations for ed25519 key management and payload signing
public interface ICryptoService
{
    // generate new ed25519 cryptographic key pair
    Result<KeyBundle> GenerateKeyPair();

    // compute digital signature for arbitrary raw string payload
    Result<string> Sign(string payload, string privateKeyBase64);

    // verify ed25519 digital signature against sender public key
    Result<bool> Verify(string payload, string signatureBase64, string publicKeyBase64);
}