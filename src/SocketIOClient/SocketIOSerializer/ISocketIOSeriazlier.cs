namespace SocketIOClient.SocketIOSerializer
{
    public interface ISocketIOSeriazlier<T>
    {
        T Read(string text);
        void Write();
    }
}
