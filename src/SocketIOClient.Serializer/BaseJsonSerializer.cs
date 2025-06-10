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

    private static void ThrowIfDataIsInvalid(object[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        if (data.Length == 0)
        {
            throw new ArgumentException("Argument must contain at least 1 item", nameof(data));
        }
    }

    private static StringBuilder NewStringBuilder(int jsonLength)
    {
        return new StringBuilder(jsonLength + 16);
    }

    private void AddPrefix(StringBuilder builder, int bytesCount, string emptyBytesPrefix, string bytesPresentPrefix)
    {
        if (bytesCount == 0)
        {
            builder.Append(emptyBytesPrefix);
        }
        else
        {
            builder.Append(bytesPresentPrefix).Append(bytesCount).Append('-');
        }

        if (!string.IsNullOrEmpty(Namespace))
        {
            builder.Append(Namespace).Append(',');
        }
    }

    public List<ProtocolMessage> Serialize(object[] data)
    {
        ThrowIfDataIsInvalid(data);
        var result = SerializeCore(data);
        var builder = NewStringBuilder(result.Json.Length);
        AddPrefix(builder, result.Bytes.Count, "42", "45");
        builder.Append(result.Json);
        return GetSerializeResult(builder.ToString(), result.Bytes);
    }

    public List<ProtocolMessage> Serialize(object[] data, int packetId)
    {
        ThrowIfDataIsInvalid(data);
        var result = SerializeCore(data);
        var builder = NewStringBuilder(result.Json.Length);
        AddPrefix(builder, result.Bytes.Count, "43", "46");
        builder.Append(packetId);
        builder.Append(result.Json);
        return GetSerializeResult(builder.ToString(), result.Bytes);
    }

    public IMessage Deserialize(string text)
    {
        // TODO: {"code":2,"message":"Bad handshake method"}
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