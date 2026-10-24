using Zapp.Core.Models;
using Zapp.Core.Services;

// instantiate packet serializer service
var serializer = new SystemJsonPacketSerializer();

// construct dummy direct packet for verification
var originalPacket = new NetworkPacket(
    PacketId: Guid.NewGuid().ToString(),
    Type: PacketType.Direct,
    SenderPublicKey: "pub_ed25519_alice",
    RecipientPublicKey: "pub_ed25519_bob",
    Ttl: 3,
    Payload: "encrypted_base64_data",
    Signature: "signature_test",
    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
);

// execute serialization pipeline
var serializedResult = serializer.Serialize(originalPacket);
if (!serializedResult.IsSuccess)
{
    // terminate on serialization failure
    Console.Error.WriteLine($"[FAIL] serialization: {serializedResult.ErrorMessage}");
    return 1;
}

// execute deserialization pipeline
var deserializedResult = serializer.Deserialize(serializedResult.Value!);
if (!deserializedResult.IsSuccess)
{
    // terminate on deserialization failure
    Console.Error.WriteLine($"[FAIL] deserialization: {deserializedResult.ErrorMessage}");
    return 1;
}

// test ttl decrement logic
var relayedPacket = deserializedResult.Value!.DecrementTtl();
if (relayedPacket.Ttl != 2 || !relayedPacket.IsRoutable)
{
    // terminate on invalid routing behavior
    Console.Error.WriteLine("[FAIL] ttl routing logic mismatch");
    return 1;
}

// print confirmation on successful test completion
Console.WriteLine("[PASS] all core domain and wire framing checks succeeded");
return 0;