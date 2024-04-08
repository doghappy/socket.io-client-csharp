﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIO.Client.Transport;

namespace SocketIO.Client.IntegrationTests
{
    [TestClass]
    public class V4WebSocketTests : SystemTextJsonTests
    {
        protected override string ServerUrl => Common.Startup.V4_WS;
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerTokenUrl => Common.Startup.V4_WS_TOKEN;
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;

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

        [TestMethod]
        public async Task Should_not_block_other_events_when_calling_Delay()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();
            io.On("1:emit", async res =>
            {
                var n = res.GetValue<int>();
                await Task.Delay(n);
                results.Add(n);
            });

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", 2000);
            await io.EmitAsync("1:emit", 1000);
            await Task.Delay(3000);

            results.Should().Equal(1000, 2000);
        }

        [TestMethod]
        public async Task Should_not_block_other_events_when_calling_Sleep()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();
            io.On("1:emit", res =>
            {
                var n = res.GetValue<int>();
                Thread.Sleep(n);
                results.Add(n);
            });

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", 2000);
            await io.EmitAsync("1:emit", 1000);
            await Task.Delay(3000);

            results.Should().Equal(1000, 2000);
        }

        [TestMethod]
        public async Task Should_not_block_other_events_when_OnAny_handler_is_called()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();
            string @event = null!;
            io.OnAny((e, res) =>
            {
                @event = e;
                var n = res.GetValue<int>();
                Thread.Sleep(n);
                results.Add(n);
            });

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", 2000);
            await io.EmitAsync("1:emit", 1000);
            await Task.Delay(3000);

            results.Should().Equal(1000, 2000);
            @event.Should().Be("1:emit");
        }

        [TestMethod]
        public async Task Should_not_block_other_events_when_Ack_Action_handler_is_called()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();

            var callback = (SocketIOResponse res) =>
            {
                var n = res.GetValue<int>();
                Thread.Sleep(n);
                results.Add(n);
            };

            await io.ConnectAsync();
            await io.EmitAsync("1:ack", callback, 2000);
            await io.EmitAsync("1:ack", callback, 1000);
            await Task.Delay(3000);

            results.Should().Equal(1000, 2000);
        }

        [TestMethod]
        public async Task Should_not_block_other_events_when_Ack_Func_handler_is_called()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();

            var callback = async (SocketIOResponse res) =>
            {
                var n = res.GetValue<int>();
                await Task.Delay(n);
                results.Add(n);
            };

            await io.ConnectAsync();
            await io.EmitAsync("1:ack", callback, 2000);
            await io.EmitAsync("1:ack", callback, 1000);
            await Task.Delay(3000);

            results.Should().Equal(1000, 2000);
        }
        
        [TestMethod]
        public async Task Should_ignore_SSL_error()
        {
            var callback = false;
            var io = new SocketIOClient("https://localhost:11404", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = TransportProtocol.WebSocket,
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
        public async Task Should_remove_FuncHandler_when_calling_Off()
        {
            var onAnyCalled = false;
            var onCalled = false;
            using var io = CreateSocketIO();
            io.OnAny((_, _) => onAnyCalled = true);
            io.On("1:emit", async (_) =>
            {
                await Task.Delay(1);
                onCalled = true;
            });
            io.Off("1:emit");

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", "test");
            await Task.Delay(100);

            onAnyCalled.Should().BeTrue();
            onCalled.Should().BeFalse();
        }
    }
}