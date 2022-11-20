using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
        [DataRow(EngineIO.V4, 1000, 9, 10)]
        [DataRow(EngineIO.V3, 1000, 9, 10)]
        public async Task Polling_ShouldWork(EngineIO eio, int delay, int min, int max)
        {
            const string url = "http://localhost:11002/socket.io/?EIO=3&transport=polling";
            const string pollingUrl = $"{url}&sid=LgtKYhIy7tUzKHH9AAAB";
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.Setup(h => h.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .Returns(async () => await Task.Delay(100));
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = eio,
            }, mockHttpPollingHandler.Object);
            await transport.ConnectAsync(new Uri(url), CancellationToken.None);
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            await Task.Delay(delay);

            mockHttpPollingHandler.Verify(e => e.GetAsync(url, CancellationToken.None), Times.Once());
            mockHttpPollingHandler.Verify(
                e => e.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());
            mockHttpPollingHandler.Verify(e => e.GetAsync(pollingUrl, CancellationToken.None),
                Times.Between(min, max, Moq.Range.Inclusive));
        }

        [TestMethod]
        [DynamicData(nameof(OnTextReceivedCases))]
        public void OnTextReceived_ShouldBecomeIMessage(EngineIO eio, string text, object expected)
        {
            IMessage msg = null;
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = eio,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (m => msg = m);
            mockHttpPollingHandler.Object.OnTextReceived(text);
            msg.Should().BeEquivalentTo(expected);
        }

        private static IEnumerable<object[]> OnTextReceivedCases => OnTextReceivedTupleCases.Select(x => new object[] { x.eio, x.text, x.expected });
        
        private static IEnumerable<(EngineIO eio, string text, object expected)> OnTextReceivedTupleCases
        {
            get
            {
                return new (EngineIO eio, string text, object expected)[]
                {
                    (
                        EngineIO.V3, 
                        "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        new
                        {
                            Type = MessageType.Opened,
                            EIO = EngineIO.V3,
                            Sid = "LgtKYhIy7tUzKHH9AAAB",
                            Upgrades = new [] { "websocket" },
                            PingInterval = 10000,
                            PingTimeout = 5000,
                        }),
                };
            }
        }

        // TODO: these tests should be transport
        // [TestMethod]
        // public void Should_Receive_Opened_With_Auth_Message()
        // {
        //     var msgs = new List<IMessage>();
        //     var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
        //     mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
        //
        //     var transport = new HttpTransport(new TransportOptions
        //     {
        //         EIO = EngineIO.V4,
        //         Auth = "{\"name\":\"admin\"}"
        //     }, mockHttpPollingHandler.Object);
        //     transport.OnReceived = (msg => msgs.Add(msg));
        //     mockHttpPollingHandler.Object.OnTextReceived(
        //         "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
        //
        //     mockHttpPollingHandler.Verify(
        //         h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40{\"name\":\"admin\"}", CancellationToken.None),
        //         Times.Once());
        //     Assert.AreEqual(1, msgs.Count);
        //     Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        // }

        // [TestMethod]
        // public async Task Eio3_Connected_Without_Namespace_Logic_Should_Work()
        // {
        //     var msgs = new List<IMessage>();
        //     var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
        //     mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
        //
        //     var transport = new HttpTransport(new TransportOptions
        //     {
        //         EIO = EngineIO.V3,
        //     }, mockHttpPollingHandler.Object);
        //     transport.OnReceived = (msg => msgs.Add(msg));
        //     mockHttpPollingHandler.Object.OnTextReceived(
        //         "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":100,\"pingTimeout\":5000}");
        //     mockHttpPollingHandler.Object.OnTextReceived("40");
        //     await Task.Delay(120);
        //
        //     mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", CancellationToken.None),
        //         Times.Once());
        //     Assert.AreEqual(3, msgs.Count);
        //     Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        //     Assert.AreEqual(MessageType.Connected, msgs[1].Type);
        //     Assert.AreEqual(MessageType.Ping, msgs[2].Type);
        // }
        //
        // [TestMethod]
        // public async Task Eio3_Connected_With_Namespace_Logic_Should_Work()
        // {
        //     var msgs = new List<IMessage>();
        //     var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
        //     mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
        //
        //     var transport = new HttpTransport(new TransportOptions
        //     {
        //         EIO = EngineIO.V3,
        //         Auth = "{\"token\":\"abc\"}",
        //         Query = new List<KeyValuePair<string, string>>
        //         {
        //             new KeyValuePair<string, string>("token", "V2")
        //         }
        //     }, mockHttpPollingHandler.Object);
        //     transport.Namespace = "/nsp";
        //     transport.OnReceived = (msg => msgs.Add(msg));
        //     mockHttpPollingHandler.Object.OnTextReceived(
        //         "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":100,\"pingTimeout\":5000}");
        //     mockHttpPollingHandler.Object.OnTextReceived("40/nsp,");
        //     await Task.Delay(120);
        //
        //     mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", CancellationToken.None),
        //         Times.Once());
        //     mockHttpPollingHandler.Verify(
        //         h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "40/nsp?token=V2,", CancellationToken.None),
        //         Times.Once());
        //     Assert.AreEqual(3, msgs.Count);
        //     Assert.AreEqual(MessageType.Opened, msgs[0].Type);
        //     Assert.AreEqual(MessageType.Connected, msgs[1].Type);
        //     Assert.AreEqual(MessageType.Ping, msgs[2].Type);
        // }
        //
        // [TestMethod]
        // public void Should_Receive_Eio3_Pong_Message()
        // {
        //     var msgs = new List<IMessage>();
        //     var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
        //     mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
        //
        //     var transport = new HttpTransport(new TransportOptions
        //     {
        //         EIO = EngineIO.V3,
        //     }, mockHttpPollingHandler.Object);
        //     transport.OnReceived = (msg => msgs.Add(msg));
        //     mockHttpPollingHandler.Object.OnTextReceived("3");
        //
        //     Assert.AreEqual(1, msgs.Count);
        //     Assert.AreEqual(MessageType.Pong, msgs[0].Type);
        //     var msg0 = msgs[0] as PongMessage;
        //
        //     Assert.AreEqual((DateTime.Now - DateTime.MinValue).TotalSeconds, msg0.Duration.TotalSeconds, 1);
        // }
        //
        // [TestMethod]
        // public void Should_Receive_Eio4_Ping_Message()
        // {
        //     var msgs = new List<IMessage>();
        //     var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
        //     mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
        //
        //     var transport = new HttpTransport(new TransportOptions
        //     {
        //         EIO = EngineIO.V4,
        //     }, mockHttpPollingHandler.Object);
        //     transport.OnReceived = (msg => msgs.Add(msg));
        //     mockHttpPollingHandler.Object.OnTextReceived("2");
        //
        //     mockHttpPollingHandler.Verify(h => h.PostAsync(null, "3", CancellationToken.None), Times.Once());
        //     Assert.AreEqual(2, msgs.Count);
        //     Assert.AreEqual(MessageType.Ping, msgs[0].Type);
        //     Assert.AreEqual(MessageType.Pong, msgs[1].Type);
        //     var msg1 = msgs[1] as PongMessage;
        //
        //     Assert.IsTrue(msg1.Duration.TotalMilliseconds is > 0 and < 10);
        // }
    }
}