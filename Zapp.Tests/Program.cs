using Zapp.Core.Models;
using Zapp.Core.Services;
using Zapp.Infrastructure.Network;

// initialize simulated node adapters
var aliceRadio = new SimulatedTransportAdapter();
var bobRadio = new SimulatedTransportAdapter();
var charlieRadio = new SimulatedTransportAdapter();

// link nodes into a shared virtual radio cell
aliceRadio.ConnectToNode(bobRadio);
aliceRadio.ConnectToNode(charlieRadio);

var bobReceivedPackets = new List<NetworkPacket>();
var charlieReceivedPackets = new List<NetworkPacket>();

bobRadio.PacketReceived += (_, packet) => bobReceivedPackets.Add(packet);
charlieRadio.PacketReceived += (_, packet) => charlieReceivedPackets.Add(packet);

// test 1: direct point-to-point transmission
var directPacket = new NetworkPacket(
    PacketId: Guid.NewGuid().ToString(),
    Type: PacketType.Direct,
    SenderPublicKey: "pub_alice",
    RecipientPublicKey: "pub_bob",
    Ttl: 3,
    Payload: "p2p_direct_message",
    Signature: "sig_direct",
    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
);

var selector = new TransportStrategySelector(new[] { aliceRadio });
var directSendResult = await selector.RoutePacketAsync(directPacket, targetAddress: "device_bob");

if (!directSendResult.IsSuccess || bobReceivedPackets.Count != 1)
{
    // terminate on direct transmission failure
    Console.Error.WriteLine("[FAIL] point-to-point direct message delivery failed");
    return 1;
}

// test 2: broadcast packet delivery
var broadcastPacket = new NetworkPacket(
    PacketId: Guid.NewGuid().ToString(),
    Type: PacketType.Broadcast,
    SenderPublicKey: "pub_alice",
    RecipientPublicKey: "0000",
    Ttl: 1,
    Payload: "emergency_broadcast_alert",
    Signature: "sig_broadcast",
    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
);

bobReceivedPackets.Clear();
charlieReceivedPackets.Clear();

var broadcastResult = await selector.RoutePacketAsync(broadcastPacket);

if (!broadcastResult.IsSuccess || bobReceivedPackets.Count != 1 || charlieReceivedPackets.Count != 1)
{
    // terminate on broadcast flood delivery failure
    Console.Error.WriteLine("[FAIL] broadcast transmission failed to reach all neighbors");
    return 1;
}

// print confirmation on overall success
Console.WriteLine("[PASS] p2p direct transport and broadcast strategy tests succeeded");
return 0;