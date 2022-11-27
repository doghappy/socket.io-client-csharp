using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4NspWebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => Common.Startup.V4_NSP_WS;
        protected override string ServerTokenUrl => Common.Startup.V4_NSP_WS_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;
    }
}