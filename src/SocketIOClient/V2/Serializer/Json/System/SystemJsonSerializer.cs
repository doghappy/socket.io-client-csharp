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
        var result = decapsulator.Decapsulate(text);
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
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
    
    private static OpenedMessage NewOpenedMessage(string text)
    {
        // TODO: Should deserializing to existing object
        // But haven't support yet. https://github.com/dotnet/runtime/issues/78556
        return JsonSerializer.Deserialize<OpenedMessage>(text, new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }
    
    private SystemJsonEventMessage NewEventMessage(string text)
    {
        var result = decapsulator.DecapsulateEventMessage(text);
        var message = new SystemJsonEventMessage
        {
            Namespace = result.Namespace,
            Id = result.Id,
        };
    
        var jsonNode = JsonNode.Parse(result.Data)!;
        var jsonArray = jsonNode.AsArray()!;
        message.Event = jsonArray[0]!.GetValue<string>();
        jsonArray.RemoveAt(0);
        message.DataItems = jsonArray;
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