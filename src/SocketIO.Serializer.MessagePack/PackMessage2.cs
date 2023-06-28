using System.Collections.Generic;
using MessagePack;

namespace SocketIO.Serializer.MessagePack;

[MessagePackObject]
public class PackMessage2
{
    public PackMessage2()
    {
        Options = new PackMessageOptions
        {
            Compress = true
        };
    }
    
    [Key("type")]
    public PackMessageType Type { get; set; }
    
    [Key("data")]
    public List<object> Data { get; set; }
    
    [Key("options")]
    public PackMessageOptions Options { get; }
    
    [Key("id")]
    public int Id { get; set; }
    
    [Key("nsp")]
    public string Nsp { get; set; }
}

[MessagePackObject]
public class PackMessageOptions
{
    [Key("compress")]
    public bool Compress { get; set; }
}