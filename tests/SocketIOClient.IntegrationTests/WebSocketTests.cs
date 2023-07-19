using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    public abstract class WebSocketTests : SocketIOTests
    {
        protected override TransportProtocol Protocol => TransportProtocol.WebSocket;
        
        protected SocketIOOptions CreateOptions()
        {
            return new SocketIOOptions
            {
                AutoUpgrade = false,
                EIO = EIO,
            };
        }

        protected override SocketIO CreateSocketIO()
        {
            return CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
            });
        }
    }
}