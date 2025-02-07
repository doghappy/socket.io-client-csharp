using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.Decapsulation;

public class DecapsulationResult
{
    public bool Success { get; set; }
    public MessageType? Type { get; set; }
    public string Data { get; set; }
}