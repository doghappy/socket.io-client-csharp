namespace SocketIOClient.Transport
{
    public class TransportMessage
    {
        public TransportMessageType Type { get; set; }
        public string Text { get; set; }
        public byte[] Binary { get; set; }
    }
}
