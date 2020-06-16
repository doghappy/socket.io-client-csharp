using SocketIOClient.NetFx.WebSocketClient;
using SocketIOClient.Packgers;

namespace SocketIOClient.WebSocketClient
{
    public class ClientWebSocket : WebSocketSharpClient
    {
        public ClientWebSocket(SocketIO io, PackgeManager parser) : base(io, parser)
        {
        }
    }
}
