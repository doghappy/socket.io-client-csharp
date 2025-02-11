using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonEventMessage : IEventMessage
{
    public JsonArray DataItems { get; set; }
    
    public MessageType Type => MessageType.Event;
    public string Namespace { get; set; }
    public string Event { get; set; }
    public int Id { get; set; }
    public JsonSerializerOptions JsonSerializerOptions { get; set; }

    public T GetDataValue<T>(int index)
    {
        return DataItems[index]!.Deserialize<T>(JsonSerializerOptions);
    }

    public object GetDataValue(Type type, int index)
    {
        return DataItems[index]!.Deserialize(type, JsonSerializerOptions);
    }
}