using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Serializer.Json.Decapsulation;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonSerializer(IDecapsulable decapsulator, JsonSerializerOptions options) : ISerializer
{
    public SystemJsonSerializer(IDecapsulable decapsulator) : this(decapsulator, new JsonSerializerOptions())
    {
    }

    public IEngineIOMessageAdapter EngineIOMessageAdapter { get; set; }
    public string Namespace { get; set; }

    private JsonSerializerOptions NewOptions(JsonConverter converter)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(converter);
        return newOptions;
    }

    public List<ProtocolMessage> Serialize(object[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        if (data.Length == 0)
        {
            throw new ArgumentException("Argument must contain at least 1 item", nameof(data));
        }
        var converter = new SystemByteArrayConverter();
        var newOptions = NewOptions(converter);
        var json = JsonSerializer.Serialize(data, newOptions);
        var builder = new StringBuilder(json.Length + 16);

        if (converter.Bytes.Count == 0)
        {
            builder.Append("42");
        }
        else
        {
            builder.Append("45").Append(converter.Bytes.Count).Append('-');
        }

        if (!string.IsNullOrEmpty(Namespace))
        {
            builder.Append(Namespace).Append(',');
        }

        // if (packetId is not null)
        // {
        //     builder.Append(packetId);
        // }

        builder.Append(json);

        return GetSerializeResult(builder.ToString(), converter.Bytes);
    }

    public IMessage Deserialize(string text)
    {
        var result = decapsulator.DecapsulateRawText(text);
        if (!result.Success)
        {
            return null;
        }

        return NewMessage(result.Type!.Value, result.Data);
    }

    private IMessage NewMessage(MessageType type, string text)
    {
        return type switch
        {
            MessageType.Opened => NewOpenedMessage(text),
            MessageType.Ping => new TypeOnlyMessage(MessageType.Ping),
            MessageType.Pong => new TypeOnlyMessage(MessageType.Pong),
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
        // Should deserializing to existing object
        // But haven't support yet. https://github.com/dotnet/runtime/issues/78556
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
        var result = decapsulator.DecapsulateEventMessage(text);
        var message = new SystemJsonEventMessage();
        SetEventMessageProperties(result, message, options);
        return message;
    }

    private ISystemJsonAckMessage NewAckMessage(string text)
    {
        var result = decapsulator.DecapsulateEventMessage(text);
        var message = new SystemJsonAckMessage();
        SetAckMessageProperties(result, message, options);
        return message;
    }

    private SystemJsonBinaryEventMessage NewBinaryEventMessage(string text)
    {
        var result = decapsulator.DecapsulateBinaryEventMessage(text);
        var message = new SystemJsonBinaryEventMessage();
        SetEventMessageProperties(result, message, options);
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
        var result = decapsulator.DecapsulateBinaryEventMessage(text);
        var message = new SystemJsonBinaryAckMessage();
        SetAckMessageProperties(result, message, options);
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

    private static List<ProtocolMessage> GetSerializeResult(string text, IEnumerable<byte[]> bytes)
    {
        var list = new List<ProtocolMessage>
        {
            new()
            {
                Type = ProtocolMessageType.Text,
                Text = text,
            },
        };
        var byteMessages = bytes.Select(item => new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = item,
        });
        list.AddRange(byteMessages);
        return list;
    }
}