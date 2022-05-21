using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2NspWebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V3_NSP_WS;
        protected override string ServerTokenUrl => V3_NSP_WS_TOKEN;
    }
}