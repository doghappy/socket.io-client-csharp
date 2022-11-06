using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2WebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V2_WS;
        protected override string ServerTokenUrl => V2_WS_TOKEN;
        protected override EngineIO EIO => EngineIO.V3;
    }
}