namespace SocketIOClient
{
    public class DisconnectReason
    {
        public const string IOServerDisconnect = "io server disconnect";
        public const string IOClientDisconnect = "io client disconnect";
        public const string PingTimeout = "ping timeout";
        public const string TransportClose = "transport close";
        public const string TransportError = "transport error";
    }
}
