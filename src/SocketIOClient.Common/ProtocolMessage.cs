namespace SocketIOClient.Common
{
    public class ProtocolMessage
    {
        public ProtocolMessageType Type { get; set; }
        public string? Text { get; set; }
        public byte[]? Bytes { get; set; }
    }
}