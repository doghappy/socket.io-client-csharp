using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2NspWebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V2_NSP_WS;
        protected override string ServerTokenUrl => V2_NSP_WS_TOKEN;
        protected override EngineIO EIO => EngineIO.V3;
    }
}