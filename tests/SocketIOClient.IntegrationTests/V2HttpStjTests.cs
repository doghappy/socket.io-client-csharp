using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpStjTests : SystemTextJsonTests
    {
        protected override EngineIO Eio => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => false;
        protected override string ServerUrl => "http://localhost:11210";
        protected override string ServerTokenUrl => "http://localhost:11211";

        [TestMethod]
        public async Task Should_automatically_upgrade_to_websocket()
        {
            var io = new SocketIO("http://localhost:11200", new SocketIOOptions
            {
                EIO = EngineIO.V3,
                AutoUpgrade = true,
                Reconnection = false,
                Transport = TransportProtocol.Polling,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            });
            await io.ConnectAsync();
            var prop = io.GetType().GetProperty("Transport", BindingFlags.Instance | BindingFlags.NonPublic);
            var transport = prop!.GetValue(io);

            io.Options.Transport.Should().Be(TransportProtocol.WebSocket);
            transport.Should().BeOfType<WebSocketTransport>();
        }
    }
}