namespace SocketIOClient.Messages
{
    public class PongMessage : IMessage
    {
        public MessageType Type => MessageType.Pong;

        public void Read(string msg)
        {
        }

        public string Write() => "3";
    }
}
