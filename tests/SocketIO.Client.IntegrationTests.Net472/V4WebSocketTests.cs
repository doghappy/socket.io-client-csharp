using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using SocketIO.Core;
using SocketIO.Client.Transport;

namespace SocketIO.Client.IntegrationTests.Net472
{
    [TestClass]
    public class V4WebSocketTests
    {
        [TestMethod]
        [DataRow("CustomHeader", "CustomHeader-Value")]
        [DataRow("User-Agent", "dotnet-socketio[client]/socket")]
        [DataRow("user-agent", "dotnet-socketio[client]/socket")]
        public async Task ExtraHeaders(string key, string value)
        {
            string actual = null;
            using (var io = new SocketIOClient(Common.Startup.V4_WS, new SocketIOOptions
            {
                Reconnection = false,
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { key, value },
                },
            }))
            {
                await io.ConnectAsync();
                await io.EmitAsync("get_header",
                    res => actual = res.GetValue<string>(),
                    key.ToLower());
                await Task.Delay(100);

                actual.Should().Be(value);
            };
        }
        
        [TestMethod]
        public async Task Should_ignore_ws_SSL_error()
        {
            var callback = false;
            var io = new SocketIOClient("https://localhost:11404", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
                RemoteCertificateValidationCallback = (sender, cert, chain, errs) =>
                {
                    callback = true;
                    return true;
                }
            });
            var connected = false;
            io.OnConnected += (s, e) => connected = true;
            await io.ConnectAsync();

            connected.Should().BeTrue();
            callback.Should().BeTrue();
        }
        
        [TestMethod]
        public async Task Should_ignore_http_SSL_error()
        {
            var callback = false;
            var io = new SocketIOClient("https://localhost:11414", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = TransportProtocol.Polling,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
                RemoteCertificateValidationCallback = (sender, cert, chain, errs) =>
                {
                    callback = true;
                    return true;
                }
            });
            var connected = false;
            io.OnConnected += (s, e) => connected = true;
            await io.ConnectAsync();

            connected.Should().BeTrue();
            callback.Should().BeTrue();
        }
    }
}
