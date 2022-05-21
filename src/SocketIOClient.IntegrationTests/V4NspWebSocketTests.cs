using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4NspWebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V4_NSP_WS;
        protected override string ServerTokenUrl => V4_NSP_WS_TOKEN;
    }
}