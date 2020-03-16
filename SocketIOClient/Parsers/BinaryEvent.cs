using SocketIOClient.Arguments;

namespace SocketIOClient.Parsers
{
    class BinaryEvent
    {
        public EventHandler EventHandler { get; set; }
        public ResponseArgs ResponseArgs { get; set; }
    }
}
