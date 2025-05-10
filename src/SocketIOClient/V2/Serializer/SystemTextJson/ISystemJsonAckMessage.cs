using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public interface ISystemJsonAckMessage : IAckMessage
{
    JsonArray DataItems { get; set; }
    JsonSerializerOptions JsonSerializerOptions { get; set; }
}