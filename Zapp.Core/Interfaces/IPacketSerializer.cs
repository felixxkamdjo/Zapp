using Zapp.Core.Common;
using Zapp.Core.Models;

namespace Zapp.Core.Interfaces;

// expose centralized contract for wire serialization
public interface IPacketSerializer
{
    // serialize packet into json string
    Result<string> Serialize(NetworkPacket packet);

    // deserialize json string into packet instance
    Result<NetworkPacket> Deserialize(string rawData);
}