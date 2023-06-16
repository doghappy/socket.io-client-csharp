namespace SocketIO.Serializer.Tests.Models;

public class FileDto
{
    public int Size { get; set; }
    public string Name { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;
}