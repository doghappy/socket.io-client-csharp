using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4NspHttpMPTests : MessagePackTests
    {
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerUrl => Common.Startup.V4_NSP_HTTP_MP;
        protected override string ServerTokenUrl => Common.Startup.V4_NSP_HTTP_TOKEN_MP;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
    }
}