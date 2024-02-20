using System.Collections.Generic;

namespace SocketIOClient
{
    public delegate void OnAnyHandler(string eventName, SocketIOResponse response);
    public delegate void OnOpenedHandler(string sid, int pingInterval, int pingTimeout);
    //public delegate void OnDisconnectedHandler(string sid, int pingInterval, int pingTimeout);
    public delegate void OnAck(int packetId, List<object> array);
    public delegate void OnBinaryAck(int packetId, int totalCount, List<object> array);
    public delegate void OnBinaryReceived(int packetId, int totalCount, string eventName, List<object> array);
    public delegate void OnDisconnected();
    public delegate void OnError(string error);
    public delegate void OnEventReceived(int packetId, string eventName, List<object> array);
    public delegate void OnOpened(string sid, int pingInterval, int pingTimeout);
    public delegate void OnPing();
    public delegate void OnPong();
}
