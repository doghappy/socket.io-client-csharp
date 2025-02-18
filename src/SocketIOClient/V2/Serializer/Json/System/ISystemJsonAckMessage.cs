using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public interface ISystemJsonAckMessage : IAckMessage
{
    JsonArray DataItems { get; set; }
    JsonSerializerOptions JsonSerializerOptions { get; set; }
}