using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer.Decapsulation;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class NewtonJsonSerializer : BaseJsonSerializer
{
    public NewtonJsonSerializer(IDecapsulable decapsulator, JsonSerializerSettings options) : base(decapsulator)
    {
        _options = options;
    }

    private readonly JsonSerializerSettings _options;
    public IEngineIOMessageAdapter EngineIOMessageAdapter { get; set; }

    protected override SerializationResult SerializeCore(object[] data)
    {
        var converter = new ByteArrayConverter();
        var newOptions = NewSettings(converter);
        var json = JsonConvert.SerializeObject(data, newOptions);
        return new SerializationResult
        {
            Json = json,
            Bytes = converter.Bytes,
        };
    }

    private JsonSerializerSettings NewSettings(JsonConverter converter)
    {
        var newOptions = new JsonSerializerSettings(_options);
        newOptions.Converters.Add(converter);
        return newOptions;
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
        return JsonConvert.DeserializeObject<OpenedMessage>(text, new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        })!;
    }

    private static void SetAckMessageProperties(
        MessageResult result,
        INewtonJsonAckMessage message,
        JsonSerializerSettings settings)
    {
        message.Namespace = result.Namespace;
        message.Id = result.Id;
        message.JsonSerializerSettings = settings;
        message.DataItems = JArray.Parse(result.Data);
    }

    private static void SetEventMessageProperties(
        MessageResult result,
        INewtonJsonEventMessage message,
        JsonSerializerSettings settings)
    {
        SetAckMessageProperties(result, message, settings);
        message.Event = message.DataItems[0].Value<string>();
        message.DataItems.RemoveAt(0);
    }

    private NewtonJsonEventMessage NewEventMessage(string text)
    {
        var result = Decapsulator.DecapsulateEventMessage(text);
        var message = new NewtonJsonEventMessage();
        SetEventMessageProperties(result, message, _options);
        return message;
    }

    private INewtonJsonAckMessage NewAckMessage(string text)
    {
        var result = Decapsulator.DecapsulateEventMessage(text);
        var message = new NewtonJsonAckMessage();
        SetAckMessageProperties(result, message, _options);
        return message;
    }

    private NewtonJsonBinaryEventMessage NewBinaryEventMessage(string text)
    {
        var result = Decapsulator.DecapsulateBinaryEventMessage(text);
        var message = new NewtonJsonBinaryEventMessage();
        SetEventMessageProperties(result, message, _options);
        SetByteProperties(message, result.BytesCount);
        return message;
    }

    private static void SetByteProperties(NewtonJsonBinaryAckMessage message, int bytesCount)
    {
        message.BytesCount = bytesCount;
        message.Bytes = new List<byte[]>(bytesCount);
    }

    private INewtonJsonAckMessage NewBinaryAckMessage(string text)
    {
        var result = Decapsulator.DecapsulateBinaryEventMessage(text);
        var message = new NewtonJsonBinaryAckMessage();
        SetAckMessageProperties(result, message, _options);
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