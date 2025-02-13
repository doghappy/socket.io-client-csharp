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
    public class V4HttpStjTests : SystemTextJsonTests
    {
        protected override EngineIO Eio => EngineIO.V4;
        protected override string ServerUrl => "http://localhost:11410";
        protected override string ServerTokenUrl => "http://localhost:11411";
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        protected override bool AutoUpgrade => false;

        [TestMethod]
        public async Task Should_ignore_SSL_error()
        {
            var callback = false;
            var io = new SocketIO("https://localhost:11414", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = TransportProtocol.Polling,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
                RemoteCertificateValidationCallback = (_, _, _, _) =>
                {
                    callback = true;
                    return true;
                }
            });
            var connected = false;
            io.OnConnected += (_, _) => connected = true;
            await io.ConnectAsync();

            connected.Should().BeTrue();
            callback.Should().BeTrue();
        }

        [TestMethod]
        public async Task Should_automatically_upgrade_to_websocket()
        {
            var io = new SocketIO("http://localhost:11400", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = true,
                Reconnection = false,
                Transport = TransportProtocol.Polling,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            });
            await io.ConnectAsync();
            await Task.Delay(100);
            var prop = io.GetType().GetProperty("Transport", BindingFlags.Instance | BindingFlags.NonPublic);
            var transport = prop!.GetValue(io);

            io.Options.Transport.Should().Be(TransportProtocol.WebSocket);
            transport.Should().BeOfType<WebSocketTransport>();
        }
    }
}