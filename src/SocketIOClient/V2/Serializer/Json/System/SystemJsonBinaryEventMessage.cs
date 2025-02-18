using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonBinaryEventMessage : SystemJsonBinaryAckMessage, ISystemJsonEventMessage
{
    public override MessageType Type => MessageType.Binary;
    public string Event { get; set; }
}