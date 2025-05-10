using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer.Decapsulation;

namespace SocketIOClient.Serializer;

public abstract class BaseJsonSerializer : ISerializer
{
    protected BaseJsonSerializer(IDecapsulable decapsulator)
    {
        Decapsulator = decapsulator;
    }

    protected IDecapsulable Decapsulator { get; }
    public string Namespace { get; set; }
    protected abstract SerializationResult SerializeCore(object[] data);

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
        var result = SerializeCore(data);
        var builder = new StringBuilder(result.Json.Length + 16);

        if (result.Bytes.Count == 0)
        {
            builder.Append("42");
        }
        else
        {
            builder.Append("45").Append(result.Bytes.Count).Append('-');
        }

        if (!string.IsNullOrEmpty(Namespace))
        {
            builder.Append(Namespace).Append(',');
        }

        builder.Append(result.Json);

        return GetSerializeResult(builder.ToString(), result.Bytes);
    }

    public IMessage Deserialize(string text)
    {
        var result = Decapsulator.DecapsulateRawText(text);
        if (!result.Success)
        {
            return null;
        }

        return NewMessage(result.Type!.Value, result.Data);
    }

    public ProtocolMessage NewPingMessage()
    {
        return new ProtocolMessage
        {
            Text = "2",
        };
    }

    public ProtocolMessage NewPongMessage()
    {
        return new ProtocolMessage
        {
            Text = "3",
        };
    }

    protected abstract IMessage NewMessage(MessageType type, string text);

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