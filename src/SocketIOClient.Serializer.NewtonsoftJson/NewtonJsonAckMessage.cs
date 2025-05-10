using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class NewtonJsonAckMessage : INewtonJsonAckMessage
{
    public JArray DataItems { get; set; }

    public virtual MessageType Type => MessageType.Ack;
    public string Namespace { get; set; }
    public int Id { get; set; }
    public JsonSerializerSettings JsonSerializerSettings { get; set; }

    protected virtual JsonSerializerSettings GetSettings()
    {
        return JsonSerializerSettings;
    }

    public virtual T? GetDataValue<T>(int index)
    {
        var settings = GetSettings();
        var serializer = JsonSerializer.Create(settings);
        return DataItems[index].ToObject<T>(serializer);
    }

    public virtual object? GetDataValue(Type type, int index)
    {
        var settings = GetSettings();
        var serializer = JsonSerializer.Create(settings);
        return DataItems[index].ToObject(type, serializer);
    }
}