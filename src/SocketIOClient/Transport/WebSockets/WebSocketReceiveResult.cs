namespace SocketIOClient.Transport.WebSockets
{
    public class WebSocketReceiveResult
    {
        public int Count { get; set; }
        public bool EndOfMessage { get; set; }
        public TransportMessageType MessageType { get; set; }
        public byte[] Buffer { get; set; }
    }
}
