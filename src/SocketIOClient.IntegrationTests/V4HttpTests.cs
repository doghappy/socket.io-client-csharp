using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4HttpTests : HttpBaseTests
    {
        protected override string ServerUrl => V4_HTTP;
        protected override string ServerTokenUrl => V4_HTTP_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;
    }
}