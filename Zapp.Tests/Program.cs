using Zapp.Core.Models;
using Zapp.Core.Services;
using Zapp.Infrastructure.Cryptography;

// initialize services
var serializer = new SystemJsonPacketSerializer();
var cryptoService = new NSecCryptoService();

// execute key generation test
var keyGenResult = cryptoService.GenerateKeyPair();
if (!keyGenResult.IsSuccess)
{
    // terminate on key generation fault
    Console.Error.WriteLine($"[FAIL] key generation: {keyGenResult.ErrorMessage}");
    return 1;
}

var aliceKeys = keyGenResult.Value!;
const string payload = "Hello peer via Zapp offline mesh!";

// execute payload signing test
var signResult = cryptoService.Sign(payload, aliceKeys.PrivateKeyBase64);
if (!signResult.IsSuccess)
{
    // terminate on signing fault
    Console.Error.WriteLine($"[FAIL] signing: {signResult.ErrorMessage}");
    return 1;
}

var signature = signResult.Value!;

// execute signature verification test
var verifyResult = cryptoService.Verify(payload, signature, aliceKeys.PublicKeyBase64);
if (!verifyResult.IsSuccess || !verifyResult.Value)
{
    // terminate on signature verification fault
    Console.Error.WriteLine($"[FAIL] verification failed or invalid signature");
    return 1;
}

// test tampering resistance
const string tamperedPayload = "Hello peer via Zapp offline mesh! (tampered)";
var tamperCheck = cryptoService.Verify(tamperedPayload, signature, aliceKeys.PublicKeyBase64);
if (tamperCheck.IsSuccess && tamperCheck.Value)
{
    // terminate on undetected tampering
    Console.Error.WriteLine("[FAIL] security breach: tampered payload was accepted");
    return 1;
}

// verify wire packet packaging with authentic signature
var packet = new NetworkPacket(
    PacketId: Guid.NewGuid().ToString(),
    Type: PacketType.Direct,
    SenderPublicKey: aliceKeys.PublicKeyBase64,
    RecipientPublicKey: "dummy_recipient_key",
    Ttl: 3,
    Payload: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload)),
    Signature: signature,
    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
);

var packetJsonResult = serializer.Serialize(packet);
if (!packetJsonResult.IsSuccess)
{
    // terminate on packet serialization error
    Console.Error.WriteLine($"[FAIL] packet serialization: {packetJsonResult.ErrorMessage}");
    return 1;
}

// print confirmation on overall success
Console.WriteLine("[PASS] cryptographic identity generation, signing, and verification tests succeeded");
return 0;