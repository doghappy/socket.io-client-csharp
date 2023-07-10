using MessagePack;

namespace SocketIO.Serializer.MessagePack;

[MessagePackObject]
public class PackMessageOptions
{
    [Key("compress")]
    public bool Compress { get; set; }
}