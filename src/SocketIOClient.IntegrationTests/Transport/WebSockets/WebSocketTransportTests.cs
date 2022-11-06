using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.IntegrationTests.Transport.WebSockets
{
    [TestClass]
    public class WebSocketTransportTests
    {
        // [TestMethod]
        public async Task Sending_And_Receiving_Should_Be_Work()
        {
            const string eventName = "event name";

            var messages = new List<IMessage>(TestHelper.TestMessages.Count);
            using var server = new WebSocketServer();
            _ = server.ListenAsync();

            using var ws = new DefaultClientWebSocket();
            using var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
            }, ws);
            transport.OnReceived = m => messages.Add(m);
            await transport.ConnectAsync(server.ServerUrl, CancellationToken.None);

            foreach (var item in TestHelper.TestMessages)
            {
                var msg = new EventMessage
                {
                    Event = eventName,
                    Json = $"[\"{item}\"]"
                };
                await transport.SendAsync(msg, CancellationToken.None);
            }
            await Task.Delay(200);
            messages
                .Should().HaveCount(TestHelper.TestMessages.Count)
                .And.Equal(TestHelper.TestMessages, (a, b) =>
                {
                    var em = a as EventMessage;
                    if (em is null)
                        return false;
                    return em.JsonElements[0].GetString() == b;
                });
        }

        // [TestMethod]
        public async Task Should_Throw_An_Exception_When_SetProxy_After_Connected()
        {
            using var server = new WebSocketServer();
            _ = server.ListenAsync();

            using var ws = new DefaultClientWebSocket();
            using var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
            }, ws);
            await transport.ConnectAsync(server.ServerUrl, CancellationToken.None);

            transport
                .Invoking(t => t.SetProxy(new WebProxy()))
                .Should().Throw<InvalidOperationException>();
        }
    }
}