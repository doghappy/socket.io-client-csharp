namespace SocketIO.Serializer.MessagePack;

public enum PackMessageType
{
    Connected,
    Disconnected,
    Event,
    Ack,
    Error,
    BinaryEvent,
    BinaryAck
}