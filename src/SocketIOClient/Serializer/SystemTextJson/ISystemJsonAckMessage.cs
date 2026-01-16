using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

public interface ISystemJsonAckMessage : IDataMessage
{
    JsonArray DataItems { get; set; }
    JsonSerializerOptions JsonSerializerOptions { get; set; }
}