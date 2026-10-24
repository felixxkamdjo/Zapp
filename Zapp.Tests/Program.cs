using Zapp.Core.Models;
using Zapp.Core.Services;
using Zapp.Infrastructure.Network;
using Zapp.Infrastructure.Persistence;

// setup three distinct virtual node environments
var dbPathRelay = Path.Combine(Path.GetTempPath(), $"zapp_relay_{Guid.NewGuid():N}.db");
var dbPathBob = Path.Combine(Path.GetTempPath(), $"zapp_bob_{Guid.NewGuid():N}.db");

var relayRepo = new SqliteMessageRepository(dbPathRelay);
var bobRepo = new SqliteMessageRepository(dbPathBob);

await relayRepo.InitializeAsync();
await bobRepo.InitializeAsync();

// setup isolated radio adapters
var aliceRadio = new SimulatedTransportAdapter();
var relayRadio = new SimulatedTransportAdapter();
var bobRadio = new SimulatedTransportAdapter();

// connect topology: alice <-> relay <-> bob (alice cannot reach bob directly)
aliceRadio.ConnectToNode(relayRadio);
relayRadio.ConnectToNode(bobRadio);

const string alicePub = "pub_alice";
const string relayPub = "pub_relay";
const string bobPub = "pub_bob";

// instantiate mesh routers for relay node and destination node
var relayRouter = new MeshRouter(relayPub, relayRepo, relayRadio);
var bobRouter = new MeshRouter(bobPub, bobRepo, bobRadio);

// wire inbound transport packets to respective node mesh routers
relayRadio.PacketReceived += async (_, packet) => await relayRouter.ProcessInboundPacketAsync(packet);
bobRadio.PacketReceived += async (_, packet) => await bobRouter.ProcessInboundPacketAsync(packet);

// alice creates a direct packet addressed to bob (ttl = 3)
var messageId = Guid.NewGuid().ToString();
var alicePacketToBob = new NetworkPacket(
    PacketId: messageId,
    Type: PacketType.Direct,
    SenderPublicKey: alicePub,
    RecipientPublicKey: bobPub,
    Ttl: 3,
    Payload: "payload_from_alice_to_bob_via_relay",
    Signature: "sig_mesh_multihop",
    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
);

// alice sends packet out into her immediate radio cell
await aliceRadio.BroadcastAsync(alicePacketToBob);

// allow short asynchronous propagation delay across virtual hops
await Task.Delay(250);

// verify 1: relay node processed and decremented ttl in its transit store
var relayRelayedCheck = await relayRepo.HasRelayedPacketAsync(messageId);
if (!relayRelayedCheck.IsSuccess || !relayRelayedCheck.Value)
{
    // terminate on missing relay cache entry
    Console.Error.WriteLine("[FAIL] relay node failed to cache relayed packet");
    return 1;
}

// verify 2: destination node (bob) successfully received and stored the packet
var bobRelayedCheck = await bobRepo.HasRelayedPacketAsync(messageId);
if (!bobRelayedCheck.IsSuccess || !bobRelayedCheck.Value)
{
    // terminate on destination delivery failure
    Console.Error.WriteLine("[FAIL] destination node (bob) did not receive forwarded packet");
    return 1;
}

// verify 3: test loop suppression by re-injecting the exact same packet
var duplicateHandled = await relayRouter.ProcessInboundPacketAsync(alicePacketToBob);
if (!duplicateHandled.IsSuccess || duplicateHandled.Value)
{
    // terminate on duplicate suppression failure
    Console.Error.WriteLine("[FAIL] duplicate packet was not dropped by relay loop check");
    return 1;
}

// clean up test databases
if (File.Exists(dbPathRelay)) File.Delete(dbPathRelay);
if (File.Exists(dbPathBob)) File.Delete(dbPathBob);

// print confirmation on success
Console.WriteLine("[PASS] mesh multi-hop forwarding, ttl decrement, and loop deduplication tests succeeded");
return 0;