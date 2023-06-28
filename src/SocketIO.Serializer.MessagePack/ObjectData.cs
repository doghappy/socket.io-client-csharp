using System.Collections.Generic;
using MessagePack;

namespace SocketIO.Serializer.MessagePack;

[MessagePackObject]
public class ObjectData
{
    [Key("sid")]
    public string Sid { get; set; }
}