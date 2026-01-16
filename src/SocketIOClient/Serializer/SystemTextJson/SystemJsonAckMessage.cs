using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

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

    public virtual T GetValue<T>(int index)
    {
        var options = GetOptions();
        return DataItems[index]!.Deserialize<T>(options);
    }

    public virtual object GetValue(Type type, int index)
    {
        var options = GetOptions();
        return DataItems[index]!.Deserialize(type, options);
    }
}