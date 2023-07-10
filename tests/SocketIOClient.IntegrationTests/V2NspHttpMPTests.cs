using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2NspHttpMPTests : HttpMPBaseTests
    {
        protected override string ServerUrl => Common.Startup.V2_NSP_HTTP_MP;
        protected override EngineIO EIO => EngineIO.V3;
        protected override string ServerTokenUrl => Common.Startup.V2_NSP_HTTP_TOKEN_MP;
    }
}