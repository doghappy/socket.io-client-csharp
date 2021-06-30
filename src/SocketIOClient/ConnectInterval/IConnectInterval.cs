namespace SocketIOClient.ConnectInterval
{
    public interface IConnectInterval
    {
        double GetDelay();
        double NextDelay();
    }
}
