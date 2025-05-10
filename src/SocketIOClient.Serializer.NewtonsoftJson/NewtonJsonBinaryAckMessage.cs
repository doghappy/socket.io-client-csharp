using System.Collections.Generic;
using Newtonsoft.Json;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class NewtonJsonBinaryAckMessage : NewtonJsonAckMessage, IBinaryAckMessage
{
    public override MessageType Type => MessageType.BinaryAck;
    public IList<byte[]> Bytes { get; set; }
    public int BytesCount { get; set; }

    protected override JsonSerializerSettings GetSettings()
    {
        var settings = new JsonSerializerSettings(JsonSerializerSettings);
        var converter = new ByteArrayConverter
        {
            Bytes = Bytes,
        };
        settings.Converters.Add(converter);
        return settings;
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