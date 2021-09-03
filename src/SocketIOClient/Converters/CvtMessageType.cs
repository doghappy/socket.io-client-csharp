namespace SocketIOClient.Converters
{
    public enum CvtMessageType
    {
        Opened,
        Ping = 2,
        Pong,
        Connected = 40,
        Disconnected,
        MessageEvent,
        MessageAck,
        MessageError,
        MessageBinary,
        MessageBinaryAck
    }
}
