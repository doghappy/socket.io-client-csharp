using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4NspWebSocketMPTests : WebSocketMpTests
    {
        protected override string ServerUrl => Common.Startup.V4_NSP_WS_MP;
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerTokenUrl => Common.Startup.V4_NSP_WS_TOKEN_MP;
    }
}