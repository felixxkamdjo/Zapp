using Zapp.Core.Models;
using Zapp.Core.Utilities;
using Zapp.Infrastructure.Network;
using Zapp.Infrastructure.Persistence;

// test rssi distance estimation and proximity zones
var distanceImmediate = BleSignalEstimator.EstimateDistanceMeters(-55);
var distanceFar = BleSignalEstimator.EstimateDistanceMeters(-85);

if (distanceImmediate <= 0 || distanceFar <= distanceImmediate)
{
    // terminate on invalid distance evaluation
    Console.Error.WriteLine("[FAIL] rssi path loss calculation mismatch");
    return 1;
}

if (BleSignalEstimator.ResolveProximityZone(-55) != "Immediate" ||
    BleSignalEstimator.ResolveProximityZone(-88) != "Far")
{
    // terminate on proximity zone mapping fault
    Console.Error.WriteLine("[FAIL] proximity zone mapping fault");
    return 1;
}

// test discovery service and event pipeline
var discoveryService = new SimulatedBleDiscoveryService();
var testDbPath = Path.Combine(Path.GetTempPath(), $"zapp_ble_test_{Guid.NewGuid():N}.db");
var repository = new SqliteMessageRepository(testDbPath);
await repository.InitializeAsync();

var discoveredCount = 0;

// subscribe to peer discovery event stream
discoveryService.PeerDiscovered += async (_, advert) =>
{
    // increment received beacon counter
    Interlocked.Increment(ref discoveredCount);

    // persist discovered node into repository
    var peer = new PeerNode(
        PublicKey: advert.PublicKeyBase64,
        DisplayName: advert.DisplayName,
        LastSeenTicks: advert.ObservedAtTicks,
        Rssi: advert.Rssi,
        IsPaired: false
    );
    await repository.UpsertPeerAsync(peer);
};

// initiate scanning
await discoveryService.StartScanningAsync();

// inject simulated beacon
var mockAdvertisement = new DiscoveredPeerAdvertisement(
    DeviceAddress: "AA:BB:CC:DD:EE:FF",
    ServiceUuid: "0000FEAA-0000-1000-8000-00805F9B34FB",
    PublicKeyBase64: "pub_peer_ble_test",
    DisplayName: "Radar Test Node",
    Rssi: -62,
    ObservedAtTicks: DateTimeOffset.UtcNow.Ticks
);

discoveryService.InjectSimulatedPeer(mockAdvertisement);

// allow short async event processing delay
await Task.Delay(100);

if (discoveredCount != 1)
{
    // terminate on missed discovery event
    Console.Error.WriteLine("[FAIL] discovery event was not received by subscriber");
    return 1;
}

// verify peer entry stored in sqlite
var peersResult = await repository.GetPeersAsync();
if (!peersResult.IsSuccess || peersResult.Value!.Count != 1)
{
    // terminate on storage persistence mismatch
    Console.Error.WriteLine("[FAIL] discovered peer was not persisted to sqlite store");
    return 1;
}

// stop scanning
await discoveryService.StopScanningAsync();

// clean up test database
if (File.Exists(testDbPath))
{
    File.Delete(testDbPath);
}

// print confirmation on overall success
Console.WriteLine("[PASS] ble signal estimation and neighbor discovery pipeline tests succeeded");
return 0;