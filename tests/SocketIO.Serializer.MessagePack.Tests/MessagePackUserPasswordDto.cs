using MessagePack;

namespace SocketIO.Serializer.MessagePack.Tests;

[MessagePackObject]
public class MessagePackUserPasswordDto
{
    [Key("User")]
    public string User { get; set; } = null!;
    
    [Key("Password")]
    public string Password { get; set; } = null!;
}