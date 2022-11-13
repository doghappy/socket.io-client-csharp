using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.UnitTest.Transport.Http
{
    [TestClass]
    public class HttpTransportTest
    {
        [TestMethod]
        public async Task Connect_ThrowException_IfDirty()
        {
            Uri uri = new("http://localhost:11002/socket.io/?EIO=3&transport=polling");
            var options = new TransportOptions();
            var mockedHandler = new Mock<IHttpPollingHandler>();
            using var transport = new HttpTransport(options, mockedHandler.Object);

            await transport.ConnectAsync(uri, CancellationToken.None);

            await transport
                .Invoking(async x => await x.ConnectAsync(uri, CancellationToken.None))
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Invalid object's current state, may need to create a new object.");
        }

        [TestMethod]
        public async Task Connect_ThrowTransportException()
        {
            Uri uri = new("http://localhost:11002/socket.io/?EIO=3&transport=polling");
            var options = new TransportOptions();
            var mockedHandler = new Mock<IHttpPollingHandler>();
            mockedHandler
                .Setup(m => m.SendAsync(
                    It.Is<HttpRequestMessage>(req => Uri.Compare(
                        req.RequestUri,
                        uri,
                        UriComponents.AbsoluteUri,
                        UriFormat.SafeUnescaped,
                        StringComparison.OrdinalIgnoreCase) == 0),
                    It.IsAny<CancellationToken>()))
                .Throws<Exception>();

            using var transport = new HttpTransport(options, mockedHandler.Object);

            using var cts = new CancellationTokenSource(100);
            await transport
                .Invoking(async x => await x.ConnectAsync(uri, cts.Token))
                .Should()
                .ThrowAsync<TransportException>()
                .WithMessage("Could not connect to '*'");
        }

        [TestMethod]
        public async Task Polling_Eio4()
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.Setup(h => h.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .Returns(async () => await Task.Delay(100));
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
            }, mockHttpPollingHandler.Object);
            Uri uri = new("http://localhost:11002/socket.io/?EIO=3&transport=polling");
            await transport.ConnectAsync(uri, CancellationToken.None);
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            await Task.Delay(1000);

            mockHttpPollingHandler.Verify(
                e => e.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());
            mockHttpPollingHandler.Verify(e => e.GetAsync(It.IsAny<string>(), CancellationToken.None),
                Times.Between(9, 10, Moq.Range.Inclusive));
        }

        [TestMethod]
        public async Task Should_Work_Even_If_Message_Is_Empty()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = msg => msgs.Add(msg);
            mockHttpPollingHandler.Object.OnTextReceived(string.Empty);
            await Task.Delay(10);

            Assert.AreEqual(0, msgs.Count);
        }

        [TestMethod]
        public void Eio3_Should_Receive_Opened_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(
                h => h.PostAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        }

        [TestMethod]
        public void Eio4_Should_Receive_Opened_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40", CancellationToken.None),
                Times.Once());
            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        }

        [TestMethod]
        public void Should_Receive_Opened_With_Auth_Message()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
                Auth = "{\"name\":\"admin\"}"
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(
                h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40{\"name\":\"admin\"}", CancellationToken.None),
                Times.Once());
            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        }

        [TestMethod]
        public async Task Eio3_Connected_Without_Namespace_Logic_Should_Work()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":100,\"pingTimeout\":5000}");
            mockHttpPollingHandler.Object.OnTextReceived("40");
            await Task.Delay(120);

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", CancellationToken.None),
                Times.Once());
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
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
                Auth = "{\"token\":\"abc\"}",
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("token", "V2")
                }
            }, mockHttpPollingHandler.Object);
            transport.Namespace = "/nsp";
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":100,\"pingTimeout\":5000}");
            mockHttpPollingHandler.Object.OnTextReceived("40/nsp,");
            await Task.Delay(120);

            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", CancellationToken.None),
                Times.Once());
            mockHttpPollingHandler.Verify(
                h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40/nsp?token=V2,", CancellationToken.None),
                Times.Once());
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
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived("3");

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
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));
            mockHttpPollingHandler.Object.OnTextReceived("2");

            mockHttpPollingHandler.Verify(h => h.PostAsync(null, "3", CancellationToken.None), Times.Once());
            Assert.AreEqual(2, msgs.Count);
            Assert.AreEqual(MessageType.Ping, msgs[0].Type);
            Assert.AreEqual(MessageType.Pong, msgs[1].Type);
            var msg1 = msgs[1] as PongMessage;

            Assert.IsTrue(msg1.Duration.TotalMilliseconds is > 0 and < 10);
        }
    }
}