using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonAckMessage : ISystemJsonAckMessage
{
    public JsonArray DataItems { get; set; }

    public virtual MessageType Type => MessageType.Ack;
    public string Namespace { get; set; }
    public int Id { get; set; }
    public JsonSerializerOptions JsonSerializerOptions { get; set; }

    protected virtual JsonSerializerOptions GetOptions()
    {
        return JsonSerializerOptions;
    }

    public virtual T GetDataValue<T>(int index)
    {
        var options = GetOptions();
        return DataItems[index]!.Deserialize<T>(options);
    }

    public virtual object GetDataValue(Type type, int index)
    {
        var options = GetOptions();
        return DataItems[index]!.Deserialize(type, options);
    }
}