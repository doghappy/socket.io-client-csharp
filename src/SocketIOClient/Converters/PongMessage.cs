namespace SocketIOClient.Converters
{
    public class PongMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.Pong;

        public void Read(string msg)
        {
        }

        public string Write() => "3";
    }
}
