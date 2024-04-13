using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2NspHttpMPTests : MessagePackTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override string ServerUrl => Common.Startup.V2_NSP_HTTP_MP;
        protected override string ServerTokenUrl => Common.Startup.V2_NSP_HTTP_TOKEN_MP;
    }
}