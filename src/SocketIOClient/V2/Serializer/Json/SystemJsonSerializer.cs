using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SocketIOClient.V2.Core;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Serializer.Decapsulation;

namespace SocketIOClient.V2.Serializer.Json;

public class SystemJsonSerializer(IDecapsulable decapsulator, JsonSerializerOptions options) : ISerializer
{
    public SystemJsonSerializer(IDecapsulable decapsulator) : this(decapsulator, new JsonSerializerOptions())
    {
    }

    public EngineIO EngineIO { get; set; }
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
    
    private static IMessage NewMessage(MessageType type, string text)
    {
        return type switch
        {
            MessageType.Opened => NewOpenedMessage(text),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
    
    private static OpenedMessage NewOpenedMessage(string text)
    {
        var message = new OpenedMessage();
        message.Sid = "sid";
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