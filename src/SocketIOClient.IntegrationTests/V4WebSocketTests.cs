using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V4_WS;
        protected override string ServerTokenUrl => V4_WS_TOKEN;
    }
}