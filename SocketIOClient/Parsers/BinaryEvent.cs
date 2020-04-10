using SocketIOClient.Arguments;

namespace SocketIOClient.Parsers
{
    public class BinaryEvent
    {
        public EventHandler EventHandler { get; set; }
        public ResponseArgs ResponseArgs { get; set; }
    }
}
