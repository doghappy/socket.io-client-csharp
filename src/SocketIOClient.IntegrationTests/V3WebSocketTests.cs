using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    // [TestClass]
    public class V3WebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => Common.Startup.V3_WS;
        protected override string ServerTokenUrl => Common.Startup.V3_WS_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;
    }
}