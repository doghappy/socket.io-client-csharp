namespace SocketIOClient.Test.SocketIOTests
{
    public interface ISocketIOCreateable
    {
        SocketIO Create(bool reconnection = false);
        string Prefix { get; }
        string Url { get; }
        string Token { get; }
    }
}
