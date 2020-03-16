using SocketIOClient.Arguments;
using System;
using System.Collections.Generic;

namespace SocketIOClient.Parsers
{
    public class ResponseTextParser
    {
        public ResponseTextParser(string ns, SocketIO socket)
        {
            Parser = new OpenedParser();
            Namespace = ns;
            Socket = socket;
        }

        public IParser Parser { get; set; }
        public string Text { get; set; }
        public string Namespace { get; }
        public SocketIO Socket { get; }

        public Action ConnectHandler { get; set; }
        public Action CloseHandler { get; set; }
        public Action<OpenedArgs> OpenHandler { get; set; }
        public Action<string, ResponseArgs> UncaughtHandler { get; set; }
        public Action<string, ResponseArgs> ReceiveHandler { get; set; }
        public Action<ResponseArgs> ErrorHandler { get; set; }
        public Queue<EventHandler> BufferHandlerQueue { get; set; }

        public void Parse() => Parser.Parse(this);
    }
}
