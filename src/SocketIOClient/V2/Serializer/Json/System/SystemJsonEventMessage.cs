using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonEventMessage : SystemJsonAckMessage, IEventMessage
{
    public override MessageType Type => MessageType.Event;
    public string Event { get; set; }
}