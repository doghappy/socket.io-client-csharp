using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.TransportTests
{
    [TestClass]
    public class HttpTransportTest
    {
        [TestMethod]
        public async Task Polling_Should_Work()
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.Setup(h => h.GetAsync(It.IsAny<string>(), CancellationToken.None)).Returns(async () => await Task.Delay(100));
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, new SocketIOOptions(), null, TestHelper.Logger);
            Uri uri = new("http://localhost:11002/socket.io/?EIO=3&transport=polling");
            await transport.ConnectAsync(uri, CancellationToken.None);
            transport.OnNext("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
            await Task.Delay(1000);

            mockHttpPollingHandler.Verify(e => e.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());
            mockHttpPollingHandler.Verify(e => e.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Between(9, 10, Moq.Range.Inclusive));
        }

        [TestMethod]
        public void Message_Is_Empty_Should_Work()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, new SocketIOOptions(), null, TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext(string.Empty);
            Assert.AreEqual(0, msgs.Count);
        }

        [TestMethod]
        public void Eio3_Should_Receive_Opened_Without_Auth_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions { EIO = 3 };
            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, options, new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        }

        [TestMethod]
        public void Eio4_Should_Receive_Opened_Without_Auth_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, new SocketIOOptions(), new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40", CancellationToken.None), Times.Once());
            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        }

        [TestMethod]
        public void Should_Receive_Opened_With_Auth_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions
            {
                Auth = new
                {
                    name = "admin"
                }
            };
            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, options, new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40{\"name\":\"admin\"}", CancellationToken.None), Times.Once());
            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        }

        [TestMethod]
        public async Task Eio3_Connected_Without_Namespace_Logic_Should_Work()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions { EIO = 3 };
            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, options, new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":100,\"pingTimeout\":5000}");
            transport.OnNext("40");
            await Task.Delay(120);

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", CancellationToken.None), Times.Once());
            Assert.AreEqual(3, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
            Assert.AreEqual(MessageType.Connected, msgs[1].Type);
            Assert.AreEqual(MessageType.Ping, msgs[2].Type);
        }

        [TestMethod]
        public async Task Eio3_Connected_With_Namespace_Logic_Should_Work()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions
            {
                EIO = 3,
                Auth = new { token = "abc" },
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("token", "V2")
                },
            };
            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, options, new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Namespace = "/nsp";
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":100,\"pingTimeout\":5000}");
            transport.OnNext("40/nsp,");
            await Task.Delay(120);

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", CancellationToken.None), Times.Once());
            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40/nsp?token=V2,", CancellationToken.None), Times.Once());
            Assert.AreEqual(3, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
            Assert.AreEqual(MessageType.Connected, msgs[1].Type);
            Assert.AreEqual(MessageType.Ping, msgs[2].Type);
        }

        [TestMethod]
        public void Should_Receive_Eio3_Pong_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions { EIO = 3 };
            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, options, new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("3");

            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Pong, msgs[0].Type);
            var msg0 = msgs[0] as PongMessage;

            Assert.AreEqual((DateTime.Now - DateTime.MinValue).TotalSeconds, msg0.Duration.TotalSeconds, 1);
        }

        [TestMethod]
        public void Should_Receive_Eio4_Ping_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var textSubject = new Subject<string>();
            var bytesSubject = new Subject<byte[]>();
            mockHttpPollingHandler.Setup(h => h.TextObservable).Returns(textSubject);
            mockHttpPollingHandler.Setup(h => h.BytesObservable).Returns(bytesSubject);

            var options = new SocketIOOptions { EIO = 4 };
            var transport = new HttpTransport(null, mockHttpPollingHandler.Object, options, new SystemTextJsonSerializer(), TestHelper.Logger);
            transport.Subscribe(msg => msgs.Add(msg));
            transport.OnNext("2");

            mockHttpPollingHandler.Verify(h => h.PostAsync(null, "3", CancellationToken.None), Times.Once());
            Assert.AreEqual(2, msgs.Count);
            Assert.AreEqual(MessageType.Ping, msgs[0].Type);
            Assert.AreEqual(MessageType.Pong, msgs[1].Type);
            var msg1 = msgs[1] as PongMessage;

            Assert.IsTrue(msg1.Duration.TotalMilliseconds > 0 && msg1.Duration.TotalMilliseconds < 10);
        }
    }
}
