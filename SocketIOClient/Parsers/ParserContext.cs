using SocketIOClient.Arguments;
using System;
using System.Collections.Generic;

namespace SocketIOClient.Parsers
{
    class ParserContext
    {
        public ParserContext()
        {
            PacketId = -1;
            Callbacks = new Dictionary<int, EventHandler>();
            EventHandlers = new Dictionary<string, EventHandlerBox>();
            BinaryEvents = new List<BinaryEvent>();
            ReceivedBuffers = new List<byte[]>();
            SendBuffers = new List<byte[]>();
            SendBufferCount = -1;
        }

        public Uri Uri { get; set; }
        public Uri WsUri { get; set; }
        public int ReceivedBufferCount { get; set; }
        public string Namespace { get; set; }
        public int PacketId { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<int, EventHandler> Callbacks { get; }
        public List<byte[]> ReceivedBuffers { get; }
        public int SendBufferCount { get; set; }
        public List<byte[]> SendBuffers { get; }

        public Action ConnectHandler { get; set; }
        public Action CloseHandler { get; set; }
        public Action<OpenedArgs> OpenHandler { get; set; }
        public Action<string, ResponseArgs> UncaughtHandler { get; set; }
        public Action<string, ResponseArgs> ReceiveHandler { get; set; }
        public Action<ResponseArgs> ErrorHandler { get; set; }
        public Dictionary<string, EventHandlerBox> EventHandlers { get; }
        public List<BinaryEvent> BinaryEvents { get; }
    }
}
