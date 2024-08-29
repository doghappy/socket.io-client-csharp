using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpMpNspTests : MessagePackTests
    {
        protected override EngineIO Eio => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => false;
        protected override string ServerUrl => "http://localhost:11212/nsp";
        protected override string ServerTokenUrl => "http://localhost:11213/nsp";
    }
}