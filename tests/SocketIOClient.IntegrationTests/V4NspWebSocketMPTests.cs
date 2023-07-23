using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4NspWebSocketMPTests : MessagePackTests
    {
        protected override EngineIO EIO => EngineIO.V4;
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
        protected override string ServerUrl => Common.Startup.V4_NSP_WS_MP;
        protected override string ServerTokenUrl => Common.Startup.V4_NSP_WS_TOKEN_MP;
    }
}