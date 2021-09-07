namespace SocketIOClient.Converters
{
    public enum CvtMessageType
    {
        Opened,
        Ping = 2,
        Pong,
        Connected = 40,
        Disconnected,
        EventMessage,
        AckMessage,
        ErrorMessage,
        BinaryMessage,
        BinaryAckMessage
    }
}
