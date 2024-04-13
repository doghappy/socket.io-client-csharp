using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4NspWebSocketTests : SystemTextJsonTests
    {
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerUrl => Common.Startup.V4_NSP_WS;
        protected override string ServerTokenUrl => Common.Startup.V4_NSP_WS_TOKEN;
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
    }
}