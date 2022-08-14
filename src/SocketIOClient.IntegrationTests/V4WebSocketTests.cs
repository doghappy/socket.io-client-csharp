using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Net.Sockets;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V4_WS;
        protected override string ServerTokenUrl => V4_WS_TOKEN;

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
        public void Should_Throw_Exception_If_Proxy_Server_Not_Start()
        {
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Proxy = new WebProxy("localhost", 6138),
                Reconnection = false,
                ConnectionTimeout = TimeSpan.FromSeconds(1)
            });
            Action action = () => io.ConnectAsync().GetAwaiter().GetResult();
            action.Should().Throw<TaskCanceledException>();
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
            Action action = () => io.ConnectAsync().Wait();
            action.Should().Throw<TaskCanceledException>();
            msgs.Should().BeEquivalentTo(new[] { $"CONNECT localhost:11400 HTTP/1.1{Environment.NewLine}Host: localhost:11400{Environment.NewLine + Environment.NewLine}" });

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