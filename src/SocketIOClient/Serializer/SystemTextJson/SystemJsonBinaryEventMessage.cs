using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

public class SystemJsonBinaryEventMessage : SystemJsonBinaryAckMessage, ISystemJsonEventMessage
{
    public override MessageType Type => MessageType.Binary;
    public string Event { get; set; } = null!;
}