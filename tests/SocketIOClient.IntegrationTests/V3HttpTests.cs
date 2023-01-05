using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    // [TestClass]
    public class V3HttpTests : HttpBaseTests
    {
        protected override string ServerUrl => Common.Startup.V3_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V3_HTTP_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;
    }
}