namespace SocketIOClient.V2.Message;

public class ConnectedMessage : IMessage
{
    public MessageType Type => MessageType.Connected;
}