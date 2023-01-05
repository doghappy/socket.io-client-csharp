using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Net.Sockets;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => Common.Startup.V4_WS;
        protected override string ServerTokenUrl => Common.Startup.V4_WS_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;

        [TestMethod]
        public async Task Should_Be_Work_Even_An_Exception_Thrown_From_Handler()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();
            io.On("1:emit", res => results.Add(6 / res.GetValue<int>()));

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", 0);
            await io.EmitAsync("1:emit", 1);
            await Task.Delay(100);

            results.Should().Equal(6);
        }

        [TestMethod]
        public async Task Should_Throw_Exception_If_Proxy_Server_Not_Start()
        {
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Proxy = new WebProxy("localhost", 6138),
                Reconnection = false,
                ConnectionTimeout = TimeSpan.FromSeconds(1)
            });
            await io.Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>();
        }

        [TestMethod]
        public async Task Should_Able_To_Connect_To_Proxy()
        {
            var msgs = new List<string>();
            _ = Task.Run(StartProxyServer);
            await Task.Delay(200);
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Proxy = new WebProxy("localhost", 6138),
                Reconnection = false,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            });
            await io.Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>();

            var uri = new Uri(Common.Startup.V4_NSP_WS);
            var uriStr = $"{uri.Host}:{uri.Port}";
            msgs.Should().BeEquivalentTo(new[] { $"CONNECT {uriStr} HTTP/1.1\r\nHost: {uriStr}\r\n\r\n" });

            void StartProxyServer()
            {
                var proxy = new TcpListener(IPAddress.Parse("127.0.0.1"), 6138);
                proxy.Start();
                byte[] bytes = new byte[256];
                while (true)
                {
                    var client = proxy.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    int i;
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        msgs.Add(System.Text.Encoding.UTF8.GetString(bytes, 0, i));
                    }
                }
            }
        }
    }
}