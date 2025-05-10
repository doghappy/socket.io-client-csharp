using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public class NewtonJsonEventMessage : NewtonJsonAckMessage, INewtonJsonEventMessage
{
    public override MessageType Type => MessageType.Event;
    public string Event { get; set; }
}