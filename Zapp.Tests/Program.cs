using Zapp.Core.Models;
using Zapp.Core.Services;
using Zapp.Infrastructure.Cryptography;
using Zapp.Infrastructure.Persistence;

// initialize test database file path
var testDbPath = Path.Combine(Path.GetTempPath(), $"zapp_test_{Guid.NewGuid():N}.db");
var repository = new SqliteMessageRepository(testDbPath);

// initialize schema tables
await repository.InitializeAsync();

// test peer upsert and query
var dummyPeer = new PeerNode(
    PublicKey: "peer_pub_charlie",
    DisplayName: "Charlie Device",
    LastSeenTicks: DateTimeOffset.UtcNow.Ticks,
    Rssi: -65,
    IsPaired: true
);

var peerUpsert = await repository.UpsertPeerAsync(dummyPeer);
if (!peerUpsert.IsSuccess)
{
    // terminate on peer save failure
    Console.Error.WriteLine($"[FAIL] peer upsert: {peerUpsert.ErrorMessage}");
    return 1;
}

var peersQuery = await repository.GetPeersAsync();
if (!peersQuery.IsSuccess || peersQuery.Value!.Count == 0)
{
    // terminate on peer fetch failure
    Console.Error.WriteLine("[FAIL] unable to retrieve stored peers");
    return 1;
}

// test outbox queue insertion and lifecycle updates
var messageId = Guid.NewGuid().ToString();
var dummyMessage = new LocalMessage(
    Id: messageId,
    SenderKey: "alice_key",
    RecipientKey: dummyPeer.PublicKey,
    EncryptedContent: "dGVzdF9wYXlsb2Fk",
    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    DeliveryStatus: MessageDeliveryStatus.Pending,
    HopsCount: 0
);

var saveMessageResult = await repository.SaveMessageAsync(dummyMessage);
if (!saveMessageResult.IsSuccess)
{
    // terminate on message save failure
    Console.Error.WriteLine($"[FAIL] message save: {saveMessageResult.ErrorMessage}");
    return 1;
}

var pendingOutbox = await repository.GetPendingOutboxMessagesAsync();
if (!pendingOutbox.IsSuccess || pendingOutbox.Value!.Count != 1)
{
    // terminate on outbox filter mismatch
    Console.Error.WriteLine("[FAIL] outbox query returned invalid count");
    return 1;
}

// update status to delivered and confirm removal from pending outbox
await repository.UpdateMessageStatusAsync(messageId, MessageDeliveryStatus.Delivered);
var updatedOutbox = await repository.GetPendingOutboxMessagesAsync();
if (updatedOutbox.Value!.Count != 0)
{
    // terminate on invalid outbox status transition
    Console.Error.WriteLine("[FAIL] outbox message was not transitioned properly");
    return 1;
}

// test relay deduplication cache logic
const string packetId = "packet_mesh_test_123";
var initialCheck = await repository.HasRelayedPacketAsync(packetId);
if (!initialCheck.IsSuccess || initialCheck.Value)
{
    // terminate on false positive relay check
    Console.Error.WriteLine("[FAIL] relay cache reported existing entry before insert");
    return 1;
}

await repository.RecordRelayedPacketAsync(packetId);
var recordedCheck = await repository.HasRelayedPacketAsync(packetId);
if (!recordedCheck.IsSuccess || !recordedCheck.Value)
{
    // terminate on failed deduplication check
    Console.Error.WriteLine("[FAIL] relay cache failed to recognize recorded packet");
    return 1;
}

// clean up temporary test database
if (File.Exists(testDbPath))
{
    File.Delete(testDbPath);
}

// print confirmation on success
Console.WriteLine("[PASS] all sqlite persistence, outbox queue, and relay cache tests succeeded");
return 0;