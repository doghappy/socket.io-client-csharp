using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2NspHttpTests : HttpBaseTests
    {
        protected override string ServerUrl => Common.Startup.V2_NSP_HTTP;
        protected override EngineIO EIO => EngineIO.V3;
        protected override string ServerTokenUrl => Common.Startup.V2_NSP_HTTP_TOKEN;
    }
}