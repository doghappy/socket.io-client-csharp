using System.Collections.Generic;
using MessagePack;

namespace SocketIO.Serializer.MessagePack;

[MessagePackObject]
public class ObjectDataMessage
{
    [Key("type")]
    public int Type { get; set; }
    
    [Key("sid")]
    public string Sid { get; set; }
        
    [Key("pingInterval")]
    public int PingInterval { get; set; }
        
    [Key("pingTimeout")]
    public int PingTimeout { get; set; }
        
    [Key("upgrades")]
    public List<string> Upgrades { get; set; }

    [Key("nsp")]
    public string Namespace { get; set; }
        
    [Key("data")]
    public object Data { get; set; }

    [Key("id")]
    public int Id { get; set; } = -1;
}