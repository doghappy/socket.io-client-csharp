
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.Decapsulation;

public class DecapsulationResult
{
    public bool Success { get; set; }
    public MessageType? Type { get; set; }
    public string Data { get; set; }
}