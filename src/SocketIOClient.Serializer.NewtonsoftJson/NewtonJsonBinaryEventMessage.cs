using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class NewtonJsonBinaryEventMessage : NewtonJsonBinaryAckMessage, INewtonJsonEventMessage
{
    public override MessageType Type => MessageType.Binary;
    public string Event { get; set; }
}