using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WebSocketMessagePackTests : MessagePackTests
    {
        protected override string ServerUrl => Common.Startup.V4_WS_MP;
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerTokenUrl => Common.Startup.V4_WS_TOKEN_MP;
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
    }
}