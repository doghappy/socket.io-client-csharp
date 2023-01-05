using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpTests : HttpBaseTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override string ServerUrl => Common.Startup.V2_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V2_HTTP_TOKEN;
    }
}