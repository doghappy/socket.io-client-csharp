namespace SocketIO.Serializer.NewtonsoftJson.Tests;

public class Depth
{
    public int Value { get; set; }
    public Depth Next { get; set; } = null!;
}