using SocketIOClient.Packgers;

namespace SocketIOClient.WebSocketClient
{
    static class WebSocketClientFactory
    {
        public static WebSocketSharpClient CreateWebSocketSharpClient(SocketIO io)
        {
            return new WebSocketSharpClient(io, new PackgeManager(io));
        }

        public static ClientWebSocket CreateClientWebSocket(SocketIO io)
        {
            return new ClientWebSocket(io, new PackgeManager(io));
        }
    }
}
