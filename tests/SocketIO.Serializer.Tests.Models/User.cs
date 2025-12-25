namespace SocketIO.Serializer.Tests.Models;

public class User
{
    public required string Name { get; init; }
    public required Address Address { get; init; }

    public static readonly User SpaceJockey = new()
    {
        Name = "Space Jockey",
        Address = new Address
        {
            Planet = "LV-223"
        }
    };
}