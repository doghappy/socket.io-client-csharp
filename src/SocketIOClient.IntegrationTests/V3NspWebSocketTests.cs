using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    // [TestClass]
    public class V3NspWebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => Common.Startup.V3_NSP_WS;
        protected override string ServerTokenUrl => Common.Startup.V3_NSP_WS_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;
    }
}