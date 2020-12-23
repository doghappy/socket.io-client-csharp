namespace SocketIOClient.ConnectInterval
{
    public interface IConnectInterval
    {
        int GetDelay();
        double NextDealy();
    }
}
