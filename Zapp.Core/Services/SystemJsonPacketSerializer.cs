using System.Text.Json;
using Zapp.Core.Common;
using Zapp.Core.Interfaces;
using Zapp.Core.Models;

namespace Zapp.Core.Services;

// handle immutable wire framing using standard json
public sealed class SystemJsonPacketSerializer : IPacketSerializer
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // encode packet to wire representation
    public Result<string> Serialize(NetworkPacket packet)
    {
        try
        {
            // convert packet model into compact json string
            var json = JsonSerializer.Serialize(packet, _options);
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            // capture serialization fault details
            return Result<string>.Failure(ex.Message);
        }
    }

    // decode wire representation to packet instance
    public Result<NetworkPacket> Deserialize(string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData))
        {
            // reject empty or whitespace input
            return Result<NetworkPacket>.Failure("payload cannot be empty");
        }

        try
        {
            // parse json text back into immutable network packet
            var packet = JsonSerializer.Deserialize<NetworkPacket>(rawData, _options);
            return packet is not null
                ? Result<NetworkPacket>.Success(packet)
                : Result<NetworkPacket>.Failure("deserialization produced null reference");
        }
        catch (Exception ex)
        {
            // capture malformed payload fault details
            return Result<NetworkPacket>.Failure(ex.Message);
        }
    }
}