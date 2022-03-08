using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.TransportTests
{
    [TestClass]
    public class WebSocketTransportTest
    {
        [TestMethod]
        public async Task Send_Payload_Its_Only_Contains_Text()
        {
            var mockWs = new Mock<IClientWebSocket>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockWs.SetupGet(e => e.TextObservable).Returns(textSubject);
            mockWs.SetupGet(e => e.BytesObservable).Returns(bytesSubject);

            var transport = new WebSocketTransport(mockWs.Object, null, null);
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
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockWs.SetupGet(e => e.TextObservable).Returns(textSubject);
            mockWs.SetupGet(e => e.BytesObservable).Returns(bytesSubject);
            
            var transport = new WebSocketTransport(mockWs.Object, null, null);
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
        public async Task Eio3_Send_Payload_Contains_Text_And_Bytes()
        {
            var mockWs = new Mock<IClientWebSocket>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockWs.SetupGet(e => e.TextObservable).Returns(textSubject);
            mockWs.SetupGet(e => e.BytesObservable).Returns(bytesSubject);
            var options = new SocketIOOptions { EIO = 3 };
            var transport = new WebSocketTransport(mockWs.Object, options, null);
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
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockWs.SetupGet(e => e.TextObservable).Returns(textSubject);
            mockWs.SetupGet(e => e.BytesObservable).Returns(bytesSubject);
            var options = new SocketIOOptions { EIO = 4 };
            var transport = new WebSocketTransport(mockWs.Object, options, null);
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
        public async Task Transport_Observable_Should_Works()
        {
            var msgs = new List<IMessage>();
            var mockWs = new Mock<IClientWebSocket>();

            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockWs.SetupGet(e => e.TextObservable).Returns(textSubject);
            mockWs.SetupGet(e => e.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions
            {
                EIO = 3,
                Transport = TransportProtocol.Polling
            };
            var transport = new WebSocketTransport(mockWs.Object, options, null);
            transport.Subscribe(msg => msgs.Add(msg));

            await transport.ConnectAsync(new Uri("http://localhost"), CancellationToken.None);
            textSubject.OnNext("42[\"hi\",\"doghappy\"]");
            textSubject.OnNext("451-[\"test\",\"doghappy\",{\"_placeholder\":true,\"num\":0}]");
            bytesSubject.OnNext(new byte[] { 255, 244, 1 });
            textSubject.OnNext("3");

            Assert.AreEqual(3, msgs.Count);

            var msg0 = msgs[0] as EventMessage;
            Assert.AreEqual("hi", msg0.Event);
            Assert.AreEqual(1, msg0.JsonElements.Count);
            Assert.AreEqual(0, msg0.BinaryCount);
            Assert.AreEqual("doghappy", msg0.JsonElements[0].GetString());
            Assert.IsNull(msg0.IncomingBytes);

            var msg1 = msgs[1] as BinaryMessage;
            Assert.AreEqual("test", msg1.Event);
            Assert.AreEqual(2, msg1.JsonElements.Count);
            Assert.AreEqual(1, msg1.BinaryCount);
            Assert.AreEqual("doghappy", msg1.JsonElements[0].GetString());
            Assert.AreEqual(1, msg1.IncomingBytes.Count);
            Assert.AreEqual(255, msg1.IncomingBytes[0][0]);
            Assert.AreEqual(244, msg1.IncomingBytes[0][1]);
            Assert.AreEqual(1, msg1.IncomingBytes[0][2]);

            Assert.AreEqual(MessageType.Pong, msgs[2].Type);
        }
    }
}
