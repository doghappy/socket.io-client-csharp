using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2NspHttpTests : SystemTextJsonTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override string ServerUrl => Common.Startup.V2_NSP_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V2_NSP_HTTP_TOKEN;
    }
}