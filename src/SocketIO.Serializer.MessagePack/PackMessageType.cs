namespace SocketIO.Serializer.MessagePack;

public static class PackMessageType
{
    public const int Connected = 0;
    public const int Disconnected = 1;
    public const int Event = 2;
    public const int Ack = 3;
    public const int Error = 4;
    public const int BinaryEvent = 5;
    public const int BinaryAck = 6;
}