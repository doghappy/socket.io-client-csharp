namespace SocketIO.Serializer.Tests.Models;

public class User
{
    public string Name { get; set; } = null!;
    public Address Address { get; set; } = null!;

    public static User SpaceJockey = new()
    {
        Name = "Space Jockey",
        Address = new Address
        {
            Planet = "LV-223"
        }
    };
}