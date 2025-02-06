namespace SocketIOClient.UnitTests.V2.Serializer;

public class FileDto
{
    public int Size { get; set; }
    public string Name { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;

    public static readonly FileDto IndexHtml = new()
    {
        Name = "index.html",
        Size = 1024,
        Bytes = "Hello World!"u8.ToArray(),
    };

    /// <summary>
    /// "NiuB" (牛B) is a slang term in Chinese, often used to describe someone or something that's really impressive or awesome.
    /// It’s like saying "cool," "amazing," or "badass" in English. 
    /// NiuB => 牛B => 🐮🍺
    /// </summary>
    public static readonly FileDto NiuB = new()
    {
        Name = "NiuB",
        Size = 666,
        Bytes = "🐮🍺"u8.ToArray(),
    };
}