using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;
using SocketIOClient.Protocol.WebSocket;

namespace SocketIOClient.IntegrationTests.Net472
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
            using (var io = new SocketIO(new Uri("http://localhost:11400"), new SocketIOOptions
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
                var data = new object[] { key.ToLower() };
                await io.EmitAsync("get_header", data, res => actual = res.GetValue<string>(0));
                await Task.Delay(100);

                actual.Should().Be(value);
            }
        }

        [TestMethod]
        public async Task WebSocket_IgnoreServerCertError_AlwaysPass()
        {
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            var callback = false;
            var io = new SocketIO(new Uri("https://localhost:11404"), new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            }, services =>
            {
                services.AddSingleton<HttpClient>(_ =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, errs) =>
                        {
                            callback = true;
                            return true;
                        }
                    };
                    return new HttpClient(handler);
                });
            });
            var connected = false;
            io.OnConnected += (s, e) => connected = true;
            await io.ConnectAsync();

            connected.Should().BeTrue();
            callback.Should().BeTrue();
        }

        [TestMethod]
        public async Task HttpClient_IgnoreServerCertError_AlwaysPass()
        {
            var callback = false;
            var io = new SocketIO(new Uri("https://localhost:11414"), new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = TransportProtocol.Polling,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            }, services =>
            {
                services.AddSingleton(new WebSocketOptions
                {
                    RemoteCertificateValidationCallback = (sender, cert, chain, errs) =>
                    {
                        callback = true;
                        return true;
                    }
                });
            });
            var connected = false;
            io.OnConnected += (s, e) => connected = true;
            await io.ConnectAsync();

            connected.Should().BeTrue();
            callback.Should().BeTrue();
        }
    }
}
