using System;
using SocketIOClient.EioHandler;

namespace SocketIOClient.Processors
{
    public class MessageContext
    {
        public string Message { get; set; }
        public string Namespace { get; set; }
        public IEioHandler EioHandler { get; set; }
        public Action<ConnectionResult> ConnectedHandler { get; set; }
        public OnAck AckHandler { get; set; }
        public OnBinaryAck BinaryAckHandler { get; set; }
        public OnBinaryReceived BinaryReceivedHandler { get; set; }
        public OnDisconnected DisconnectedHandler { get; set; }
        public OnError ErrorHandler { get; set; }
        public OnEventReceived EventReceivedHandler { get; set; }
        public OnOpened OpenedHandler { get; set; }
        public OnPing PingHandler { get; set; }
        public OnPong PongHandler { get; set; }
    }
}
