namespace SocketIOClient.Converters
{
    public class PingMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.Ping;

        public void Read(string msg)
        {
        }

        public string Write() => "2";
    }
}
