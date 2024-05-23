using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WsStjNspTests : SystemTextJsonTests
    {
        protected override EngineIO Eio => EngineIO.V4;
        protected override string ServerUrl => "http://localhost:11400/nsp";
        protected override string ServerTokenUrl => "http://localhost:11401/nsp";
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
        protected override bool AutoUpgrade => false;
    }
}