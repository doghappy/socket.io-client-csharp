using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpStjNspAuTests : SystemTextJsonTests
    {
        protected override EngineIO Eio => EngineIO.V3;
        protected override string ServerUrl => "http://localhost:11200/nsp";
        protected override string ServerTokenUrl => "http://localhost:11201/nsp";
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => true;
    }
}