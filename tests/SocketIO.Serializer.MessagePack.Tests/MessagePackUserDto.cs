using MessagePack;

namespace SocketIO.Serializer.MessagePack.Tests;

[MessagePackObject]
public class MessagePackUserDto
{
    [Key("username")]
    public string Username { get; set; } = null!;

    [Key("email")]
    public string Email { get; set; } = null!;
}
