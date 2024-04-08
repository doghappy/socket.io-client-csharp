using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIO.Client.Transport;

namespace SocketIO.Client.IntegrationTests
{
    [TestClass]
    public class V4HttpTests : SystemTextJsonTests
    {
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerUrl => Common.Startup.V4_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V4_HTTP_TOKEN;
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        
        [TestMethod]
        public async Task Should_ignore_SSL_error()
        {
            var callback = false;
            var io = new SocketIOClient("https://localhost:11414", new SocketIOOptions
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
    }
}