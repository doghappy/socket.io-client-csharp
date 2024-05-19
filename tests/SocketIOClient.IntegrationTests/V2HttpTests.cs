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
    public class V2HttpTests : SystemTextJsonTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override string ServerUrl => Common.Startup.V2_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V2_HTTP_TOKEN;
        
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