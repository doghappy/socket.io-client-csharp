namespace SocketIO.Serializer.Tests.Models;

public class FileDto
{
    public int Size { get; set; }
    public string Name { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;

    public static FileDto IndexHtml = new()
    {
        Name = "index.html",
        Size = 1024,
        Bytes = "Hello World!"u8.ToArray()
    };

    /// <summary>
    /// Niubility is a popular Chinglish word to describe great ability.
    /// Niubility => 牛比 => 🐮🍺
    /// </summary>
    public static FileDto Niubility = new()
    {
        Name = "Niubility",
        Size = 666,
        Bytes = "🐮🍺"u8.ToArray()
    };
}