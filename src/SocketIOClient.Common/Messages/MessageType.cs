namespace SocketIOClient.Common.Messages
{
    public enum MessageType
    {
        Opened,
        Ping = 2,
        Pong,
        Connected = 40,
        Disconnected,
        Event,
        Ack,
        Error,
        Binary,
        BinaryAck,
    }
}