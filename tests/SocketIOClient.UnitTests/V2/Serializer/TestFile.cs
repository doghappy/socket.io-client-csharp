namespace SocketIOClient.UnitTests.V2.Serializer;

public class TestFile
{
    public int Size { get; set; }
    public string Name { get; set; } = null!;
    public byte[] Bytes { get; set; } = null!;

    public static readonly TestFile IndexHtml = new()
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
    public static readonly TestFile NiuB = new()
    {
        Name = "NiuB",
        Size = 666,
        Bytes = "🐮🍺"u8.ToArray(),
    };
}