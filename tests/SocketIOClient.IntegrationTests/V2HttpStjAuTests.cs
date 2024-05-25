using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpStjAuTests : SystemTextJsonTests
    {
        protected override EngineIO Eio => EngineIO.V4;
        protected override string ServerUrl => "http://localhost:11200";
        protected override string ServerTokenUrl => "http://localhost:11201";
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => true;
    }
}