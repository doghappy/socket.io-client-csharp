using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.IntegrationTests.Transport.WebSockets
{
    // [TestClass]
    public class SystemNetWebSocketsClientWebSocketTests
    {
        [TestMethod]
        public async Task Send_And_Receive_Should_Be_Work()
        {
            using var server = new WebSocketServer();
            _ = server.ListenAsync();

            using var client = new DefaultClientWebSocket();
            await client.ConnectAsync(server.ServerUrl, CancellationToken.None);

            foreach (var item in TestHelper.TestMessages)
            {
                var bytes = Encoding.UTF8.GetBytes(item);
                await client.SendAsync(bytes, TransportMessageType.Text, true, CancellationToken.None);
                var buffer = new byte[1024];
                var result = await client.ReceiveAsync(ChunkSize.Size8K, CancellationToken.None);
                result.Should().BeEquivalentTo(new WebSocketReceiveResult
                {
                    Count = bytes.Length,
                    EndOfMessage = result.EndOfMessage,
                    MessageType = TransportMessageType.Text,
                });
            }

            TestHelper.TestMessages.Should().HaveCountGreaterThan(0);
        }

        [TestMethod]
        public async Task State_Should_Be_Correct()
        {
            using var server = new WebSocketServer();
            server.Start();
            _ = server.ListenAsync();
            
            Console.WriteLine($"URL:{server.ServerUrl}");

            using var ws = new DefaultClientWebSocket();
            ws.State.Should().Be(WebSocketState.None);

            await ws.ConnectAsync(server.ServerUrl, CancellationToken.None);
            ws.State.Should().Be(WebSocketState.Open);

            await ws.DisconnectAsync(CancellationToken.None);
            ws.State.Should().Be(WebSocketState.Closed);

            server.AbortAll();
            ws.State.Should().Be(WebSocketState.Aborted);
        }
    }
}