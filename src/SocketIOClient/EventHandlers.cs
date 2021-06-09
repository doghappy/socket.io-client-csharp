namespace SocketIOClient
{
    public delegate void OnAnyHandler(string eventName, SocketIOResponse response);
    public delegate void OnOpenedHandler(string sid, int pingInterval, int pingTimeout);
    public delegate void OnDisconnectedHandler(string sid, int pingInterval, int pingTimeout);
}
