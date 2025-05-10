using MessagePack;

namespace SocketIO.Serializer.MessagePack;

[MessagePackObject]
public class GenericMessage
{
    [Key("type")]
    public int Type => PackMessageType.Connected;

    [Key("data")]
    public object Data { get; set; }

    [Key("nsp")]
    public string Nsp { get; set; }
}