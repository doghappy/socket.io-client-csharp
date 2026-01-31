using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.Common.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class NewtonJsonAckMessage : INewtonJsonAckMessage
{
    public JArray DataItems { get; set; } = null!;

    public virtual MessageType Type => MessageType.Ack;
    public string? Namespace { get; set; }
    public int Id { get; set; }
    public string RawText { get; set; } = null!;
    public JsonSerializerSettings JsonSerializerSettings { get; set; } = null!;

    protected virtual JsonSerializerSettings GetSettings()
    {
        return JsonSerializerSettings;
    }

    public virtual T? GetValue<T>(int index)
    {
        var settings = GetSettings();
        var serializer = JsonSerializer.Create(settings);
        return DataItems[index].ToObject<T>(serializer);
    }

    public virtual object? GetValue(Type type, int index)
    {
        var settings = GetSettings();
        var serializer = JsonSerializer.Create(settings);
        return DataItems[index].ToObject(type, serializer);
    }
}