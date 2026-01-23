using System.Collections.Generic;
using System.Text.Json;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

public class SystemJsonBinaryAckMessage : SystemJsonAckMessage, IBinaryAckMessage
{
    public override MessageType Type => MessageType.BinaryAck;
    public IList<byte[]> Bytes { get; set; } = [];
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

    public bool ReadyDelivery => BytesCount == Bytes.Count;

    public void Add(byte[] bytes)
    {
        Bytes.Add(bytes);
    }
}