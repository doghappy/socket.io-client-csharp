using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTest.TransportTests
{
    [TestClass]
    public class WebSocketTransportTest
    {
        [TestMethod]
        public async Task Send_Payload_Its_Only_Contains_Text()
        {
            var mockWs = new Mock<IClientWebSocket>();
            var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V4
            }, mockWs.Object);
            var payload = new Payload
            {
                Text = "hello"
            };

            await transport.SendAsync(payload, CancellationToken.None);

            mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, true, CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task Send_Payload_Its_Only_Contains_10KB_Text()
        {
            var mockWs = new Mock<IClientWebSocket>();

            var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V4
            }, mockWs.Object);
            var payload = new Payload
            {
                Text = new string('a', 10 * 1024)
            };

            await transport.SendAsync(payload, CancellationToken.None);

            mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, It.IsAny<bool>(), CancellationToken.None), Times.Exactly(2));
            mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, true, CancellationToken.None), Times.Once());
            mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, false, CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task Should_be_able_to_send_payload_which_contains_text_and_bytes()
        {
            var mockWs = new Mock<IClientWebSocket>();
            var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V3
            }, mockWs.Object);
            var payload = new Payload
            {
                Text = "hello"
            };
            payload.Bytes = new List<byte[]>();
            var b0 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var b1 = new byte[] { 11, 12, 13, 14 };
            payload.Bytes.Add(b0);
            payload.Bytes.Add(b1);

            var eb0 = new byte[b0.Length + 1];
            var eb1 = new byte[b1.Length + 1];
            eb0[0] = 4;
            eb1[0] = 4;
            Buffer.BlockCopy(b0, 0, eb0, 1, b0.Length);
            Buffer.BlockCopy(b1, 0, eb1, 1, b1.Length);

            await transport.SendAsync(payload, CancellationToken.None);

            mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, true, CancellationToken.None), Times.Once());
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, eb0)), TransportMessageType.Binary, true, CancellationToken.None), Times.Once());
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, eb1)), TransportMessageType.Binary, true, CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task Eio4_Send_Payload_Contains_Text_And_Bytes()
        {
            var mockWs = new Mock<IClientWebSocket>();

            var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V4
            }, mockWs.Object);
            var payload = new Payload
            {
                Text = "hello"
            };
            payload.Bytes = new List<byte[]>();
            var b0 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var b1 = new byte[] { 11, 12, 13, 14 };
            payload.Bytes.Add(b0);
            payload.Bytes.Add(b1);

            await transport.SendAsync(payload, CancellationToken.None);

            mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, true, CancellationToken.None), Times.Once());
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, b0)), TransportMessageType.Binary, true, CancellationToken.None), Times.Once());
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, b1)), TransportMessageType.Binary, true, CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task Should_be_able_to_work_concurrently()
        {
            var mockWs = new Mock<IClientWebSocket>();

            int order = 0;
            mockWs
                .Setup(x => x.SendAsync(It.Is<byte[]>(b => b.Length == ChunkSize.Size8K), TransportMessageType.Text, false, CancellationToken.None))
                .Callback(() => (++order % 4).Should().Be(1));
            mockWs
                .Setup(x => x.SendAsync(It.Is<byte[]>(b => b.Length == 1), TransportMessageType.Text, true, CancellationToken.None))
                .Callback(() => (++order % 4).Should().Be(2));
            mockWs
                .Setup(x => x.SendAsync(It.Is<byte[]>(b => b.Length == ChunkSize.Size8K), TransportMessageType.Binary, false, CancellationToken.None))
                .Callback(() => (++order % 4).Should().Be(3));
            mockWs
                .Setup(x => x.SendAsync(It.Is<byte[]>(b => b.Length == 1), TransportMessageType.Binary, true, CancellationToken.None))
                .Callback(() => (++order % 4).Should().Be(0));

            var transport = new WebSocketTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
            }, mockWs.Object);
            var payload = new Payload
            {
                Text = new string('a', ChunkSize.Size8K + 1),
            };
            payload.Bytes = new List<byte[]>();
            var bytes = Encoding.UTF8.GetBytes(payload.Text);
            payload.Bytes.Add(bytes);

            const int toExclusive = 100;
            Parallel.For(0, toExclusive, _ => transport.SendAsync(payload, CancellationToken.None).GetAwaiter().GetResult());

            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == ChunkSize.Size8K), TransportMessageType.Text, false, CancellationToken.None), Times.Exactly(toExclusive));
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == 1), TransportMessageType.Text, true, CancellationToken.None), Times.Exactly(toExclusive));
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == ChunkSize.Size8K), TransportMessageType.Binary, false, CancellationToken.None), Times.Exactly(toExclusive));
            mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == 1), TransportMessageType.Binary, true, CancellationToken.None), Times.Exactly(toExclusive));
        }
    }
}