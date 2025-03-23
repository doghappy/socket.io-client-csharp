using System.Collections.Generic;
using System.Text.Json;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public class SystemJsonBinaryAckMessage : SystemJsonAckMessage, IBinaryAckMessage
{
    public override MessageType Type => MessageType.BinaryAck;
    public IList<byte[]> Bytes { get; set; }
    public int BytesCount { get; set; }

    protected override JsonSerializerOptions GetOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions);
        var converter = new ByteArrayConverter
        {
            Bytes = Bytes,
        };
        options.Converters.Add(converter);
        return options;
    }

    public bool ReadyDelivery
    {
        get
        {
            if (Bytes is null)
            {
                return false;
            }
            return BytesCount == Bytes.Count;
        }
    }

    public void Add(byte[] bytes)
    {
        Bytes ??= new List<byte[]>(BytesCount);
        Bytes.Add(bytes);
    }
}