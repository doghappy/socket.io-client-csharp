namespace SocketIOClient.Packgers
{
    public interface IUnpackable
    {
        void Unpack(SocketIO client, string text);
    }
}
