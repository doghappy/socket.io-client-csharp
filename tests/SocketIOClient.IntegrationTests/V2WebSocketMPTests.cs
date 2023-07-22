using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2WebSocketMPTests : MessagePackTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
        protected override string ServerUrl => Common.Startup.V2_WS_MP;
        protected override string ServerTokenUrl => Common.Startup.V2_WS_TOKEN_MP;
    }
}