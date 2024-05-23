using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WsMpTests : MessagePackTests
    {
        protected override string ServerUrl => "http://localhost:11402";
        protected override EngineIO Eio => EngineIO.V4;
        protected override string ServerTokenUrl => "http://localhost:11403";
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
        protected override bool AutoUpgrade => false;
    }
}