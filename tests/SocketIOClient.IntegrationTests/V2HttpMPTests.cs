using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpMPTests : HttpMPBaseTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override string ServerUrl => Common.Startup.V2_HTTP_MP;
        protected override string ServerTokenUrl => Common.Startup.V2_HTTP_TOKEN_MP;
    }
}