using System.Text;
using NSec.Cryptography;
using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.Infrastructure.Cryptography;

// handle ed25519 operations via nsec library
public sealed class NSecCryptoService : ICryptoService
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    // generate fresh cryptographic identity
    public Result<KeyBundle> GenerateKeyPair()
    {
        try
        {
            // create new private signing key
            using var key = Key.Create(Algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

            // export raw key bytes
            var privateKeyBytes = key.Export(KeyBlobFormat.RawPrivateKey);
            var publicKeyBytes = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

            // package keys into immutable bundle
            var bundle = new KeyBundle(
                PublicKeyBase64: Convert.ToBase64String(publicKeyBytes),
                PrivateKeyBase64: Convert.ToBase64String(privateKeyBytes),
                CreatedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );

            return Result<KeyBundle>.Success(bundle);
        }
        catch (Exception ex)
        {
            // capture key generation failure
            return Result<KeyBundle>.Failure(ex.Message);
        }
    }

    // sign raw data using sender private key
    public Result<string> Sign(string payload, string privateKeyBase64)
    {
        try
        {
            // decode private key bytes
            var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

            // import raw private key for signing
            using var key = Key.Import(Algorithm, privateKeyBytes, KeyBlobFormat.RawPrivateKey);

            // encode string payload into byte array
            var data = Encoding.UTF8.GetBytes(payload);

            // compute digital signature bytes
            var signatureBytes = Algorithm.Sign(key, data);

            // return signature as base64 string
            return Result<string>.Success(Convert.ToBase64String(signatureBytes));
        }
        catch (Exception ex)
        {
            // capture signing failure
            return Result<string>.Failure(ex.Message);
        }
    }

    // verify payload integrity and authenticity
    public Result<bool> Verify(string payload, string signatureBase64, string publicKeyBase64)
    {
        try
        {
            // decode public key and signature bytes
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            var signatureBytes = Convert.FromBase64String(signatureBase64);

            // import raw public key
            var publicKey = PublicKey.Import(Algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey);

            // encode string payload into byte array
            var data = Encoding.UTF8.GetBytes(payload);

            // perform signature verification
            var isValid = Algorithm.Verify(publicKey, data, signatureBytes);

            return Result<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            // capture verification error
            return Result<bool>.Failure(ex.Message);
        }
    }
}