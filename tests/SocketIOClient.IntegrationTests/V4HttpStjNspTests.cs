using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4HttpStjNspTests : SystemTextJsonTests
    {
        protected override EngineIO Eio => EngineIO.V4;
        protected override string ServerUrl => "http://localhost:11410/nsp";
        protected override string ServerTokenUrl => "http://localhost:11411/nsp";
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => false;
    }
}