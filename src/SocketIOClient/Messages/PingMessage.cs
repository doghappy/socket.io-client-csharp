namespace SocketIOClient.Messages
{
    public class PingMessage : IMessage
    {
        public MessageType Type => MessageType.Ping;

        public void Read(string msg)
        {
        }

        public string Write() => "2";
    }
}
