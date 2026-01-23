using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

public class SystemJsonEventMessage : SystemJsonAckMessage, ISystemJsonEventMessage
{
    public override MessageType Type => MessageType.Event;
    public string Event { get; set; } = null!;
}