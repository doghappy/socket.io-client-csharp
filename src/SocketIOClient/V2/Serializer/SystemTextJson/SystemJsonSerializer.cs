using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public class SystemJsonSerializer : BaseJsonSerializer
{
    public SystemJsonSerializer(IDecapsulable decapsulator) : base(decapsulator)
    {
        JsonSerializerOptions = new JsonSerializerOptions();
    }

    public JsonSerializerOptions JsonSerializerOptions { get; set; }
    public IEngineIOMessageAdapter EngineIOMessageAdapter { get; set; }

    private JsonSerializerOptions NewOptions(JsonConverter converter)
    {
        var newOptions = new JsonSerializerOptions(JsonSerializerOptions);
        newOptions.Converters.Add(converter);
        return newOptions;
    }

    protected override SerializationResult SerializeCore(object[] data)
    {
        var converter = new ByteArrayConverter();
        var newOptions = NewOptions(converter);
        var json = JsonSerializer.Serialize(data, newOptions);
        return new SerializationResult
        {
            Json = json,
            Bytes = converter.Bytes,
        };
    }

    protected override IMessage NewMessage(MessageType type, string text)
    {
        return type switch
        {
            MessageType.Opened => NewOpenedMessage(text),
            MessageType.Ping => new PingMessage(),
            MessageType.Pong => new PongMessage(),
            MessageType.Connected => EngineIOMessageAdapter.DeserializeConnectedMessage(text),
            MessageType.Disconnected => NewDisconnectedMessage(text),
            MessageType.Event => NewEventMessage(text),
            MessageType.Ack => NewAckMessage(text),
            MessageType.Error => EngineIOMessageAdapter.DeserializeErrorMessage(text),
            MessageType.Binary => NewBinaryEventMessage(text),
            MessageType.BinaryAck => NewBinaryAckMessage(text),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    private static OpenedMessage NewOpenedMessage(string text)
    {
        return JsonSerializer.Deserialize<OpenedMessage>(text, new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    private static void SetAckMessageProperties(
        MessageResult result,
        ISystemJsonAckMessage message,
        JsonSerializerOptions options)
    {
        message.Namespace = result.Namespace;
        message.Id = result.Id;
        message.JsonSerializerOptions = options;

        var jsonNode = JsonNode.Parse(result.Data)!;
        var jsonArray = jsonNode.AsArray()!;
        message.DataItems = jsonArray;
    }

    private static void SetEventMessageProperties(
        MessageResult result,
        ISystemJsonEventMessage message,
        JsonSerializerOptions options)
    {
        SetAckMessageProperties(result, message, options);
        message.Event = message.DataItems[0]!.GetValue<string>();
        message.DataItems.RemoveAt(0);
    }

    private SystemJsonEventMessage NewEventMessage(string text)
    {
        var result = Decapsulator.DecapsulateEventMessage(text);
        var message = new SystemJsonEventMessage();
        SetEventMessageProperties(result, message, JsonSerializerOptions);
        return message;
    }

    private ISystemJsonAckMessage NewAckMessage(string text)
    {
        var result = Decapsulator.DecapsulateEventMessage(text);
        var message = new SystemJsonAckMessage();
        SetAckMessageProperties(result, message, JsonSerializerOptions);
        return message;
    }

    private SystemJsonBinaryEventMessage NewBinaryEventMessage(string text)
    {
        var result = Decapsulator.DecapsulateBinaryEventMessage(text);
        var message = new SystemJsonBinaryEventMessage();
        SetEventMessageProperties(result, message, JsonSerializerOptions);
        SetByteProperties(message, result.BytesCount);
        return message;
    }

    private static void SetByteProperties(SystemJsonBinaryAckMessage message, int bytesCount)
    {
        message.BytesCount = bytesCount;
        message.Bytes = new List<byte[]>(bytesCount);
    }

    private ISystemJsonAckMessage NewBinaryAckMessage(string text)
    {
        var result = Decapsulator.DecapsulateBinaryEventMessage(text);
        var message = new SystemJsonBinaryAckMessage();
        SetAckMessageProperties(result, message, JsonSerializerOptions);
        SetByteProperties(message, result.BytesCount);
        return message;
    }

    private static DisconnectedMessage NewDisconnectedMessage(string text)
    {
        var message = new DisconnectedMessage();
        if (!string.IsNullOrEmpty(text))
        {
            message.Namespace = text.TrimEnd(',');
        }
        return message;
    }
}