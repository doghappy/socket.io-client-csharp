using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4HttpMpTests : MessagePackTests
    {
        protected override EngineIO Eio => EngineIO.V4;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => false;
        protected override string ServerUrl => "http://localhost:11412";
        protected override string ServerTokenUrl => "http://localhost:11413";
    }
}