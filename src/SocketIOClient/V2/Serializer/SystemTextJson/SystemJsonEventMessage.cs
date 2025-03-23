using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public class SystemJsonEventMessage : SystemJsonAckMessage, ISystemJsonEventMessage
{
    public override MessageType Type => MessageType.Event;
    public string Event { get; set; }
}