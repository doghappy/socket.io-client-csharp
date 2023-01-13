using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTest
{
    [TestClass]
    public class SocketIOTest
    {
        [TestMethod]
        [DynamicData(nameof(ConnectCases))]
        public async Task Should_be_able_to_connect(SocketIOOptions options, string[] res)
        {
            int i = -1;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var text = GetResponseText(ref i, res);
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(text, new MediaTypeHeaderValue("text/pain")),
                    };
                });
            var mockWs = new Mock<IClientWebSocket>();
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() =>
                {
                    var text = GetResponseText(ref i, res);
                    var bytes = Encoding.UTF8.GetBytes(text);
                    return new WebSocketReceiveResult
                    {
                        MessageType = TransportMessageType.Text,
                        Buffer = bytes,
                        EndOfMessage = true,
                        Count = bytes.Length,
                    };
                });
            using var io = new SocketIO("http://localhost:11002", options);
            io.HttpClientProvider = ()=> mockHttp.Object;
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io.ConnectAsync();

            io.Connected.Should().BeTrue();
        }

        private static IEnumerable<object[]> ConnectCases =>
            ConnectTupleCases.Select(x => new object[] { x.options, x.res });

        private static IEnumerable<(SocketIOOptions options, string[] res)> ConnectTupleCases
        {
            get
            {
                return new (SocketIOOptions options, string[] res)[]
                {
                    (new SocketIOOptions
                    {
                        EIO = EngineIO.V3,
                        Transport = TransportProtocol.Polling,
                    }, new[]
                    {
                        "85:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        "2:40"
                    }),
                    (new SocketIOOptions
                    {
                        EIO = EngineIO.V3,
                        Transport = TransportProtocol.WebSocket,
                    }, new[]
                    {
                        "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}"
                    }),
                    (new SocketIOOptions
                    {
                        EIO = EngineIO.V4,
                        Transport = TransportProtocol.Polling,
                    }, new[]
                    {
                        "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}"
                    }),
                    (new SocketIOOptions
                    {
                        EIO = EngineIO.V4,
                        Transport = TransportProtocol.WebSocket,
                    }, new[]
                    {
                        "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}"
                    }),
                };
            }
        }

        private static string GetResponseText(ref int i, string[] messages)
        {
            i++;
            if (i < messages.Length)
            {
                return messages[i];
            }

            Task.Delay(1000).Wait();
            return string.Empty;
        }

        [TestMethod]
        [DynamicData(nameof(UpgradeToWebSocketCases))]
        public async Task Should_upgrade_transport(EngineIO eio, string handshake, string[] messages)
        {
            int i = -1;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.GetStringAsync(It.IsAny<Uri>()))
                .ReturnsAsync(handshake);
            var mockWs = new Mock<IClientWebSocket>();
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() =>
                {
                    var text = GetResponseText(ref i, messages);
                    var bytes = Encoding.UTF8.GetBytes(text);
                    return new WebSocketReceiveResult
                    {
                        MessageType = TransportMessageType.Text,
                        Buffer = bytes,
                        EndOfMessage = true,
                        Count = bytes.Length,
                    };
                });
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                AutoUpgrade = true,
                Transport = TransportProtocol.Polling,
                EIO = eio,
            });
            io.HttpClientProvider = ()=> mockHttp.Object;
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io.ConnectAsync();

            io.Connected.Should().BeTrue();
            io.Options.Transport.Should().Be(TransportProtocol.WebSocket);
        }

        private static IEnumerable<object[]> UpgradeToWebSocketCases =>
            UpgradeToWebSocketTupleCases.Select(x => new object[] { x.eio, x.handshake, x.messages });
        
        private static IEnumerable<(EngineIO eio, string handshake, string[] messages)> UpgradeToWebSocketTupleCases
        {
            get
            {
                return new (EngineIO eio, string handshake, string[] messages)[]
                {
                    (
                        EngineIO.V3,
                        "85:0{\"sid\":\"LgtKYhIy7\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        new[]
                        {
                            "0{\"sid\":\"test\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "40",
                        }),
                    (
                        EngineIO.V3,
                        "test_websocket_1",
                        new[]
                        {
                            "0{\"sid\":\"test\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "40",
                        }),
                    (
                        EngineIO.V4,
                        "0{\"sid\":\"LgtKYhIy7\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        new[]
                        {
                            "0{\"sid\":\"test\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "40{\"sid\":\"hello\"}"
                        }),
                    (
                        EngineIO.V4,
                        "test_websocket_1",
                        new[]
                        {
                            "0{\"sid\":\"test\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "40{\"sid\":\"hello\"}"
                        }),
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(NotUpgradeToWebSocketCases))]
        public async Task Should_not_upgrade_transport(SocketIOOptions options, string handshake, string[] messages)
        {
            int i = -1;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.GetStringAsync(It.IsAny<Uri>()))
                .ReturnsAsync(handshake);
            mockHttp
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var text = GetResponseText(ref i, messages);
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(text, new MediaTypeHeaderValue("text/pain")),
                    };
                });
            using var io = new SocketIO("http://localhost:11002", options);
            io.HttpClientProvider = ()=> mockHttp.Object;

            await io.ConnectAsync();

            io.Connected.Should().BeTrue();
            io.Options.Transport.Should().Be(TransportProtocol.Polling);
        }
        
         private static IEnumerable<object[]> NotUpgradeToWebSocketCases =>
            NotUpgradeToWebSocketTupleCases.Select(x => new object[] { x.options, x.handshake, x.messages });
        
        private static IEnumerable<(SocketIOOptions options, string handshake, string[] messages)> NotUpgradeToWebSocketTupleCases
        {
            get
            {
                return new (SocketIOOptions options, string handshake, string[] messages)[]
                {
                    (
                        new SocketIOOptions
                        {
                            EIO = EngineIO.V3,
                            AutoUpgrade = false,
                            Transport = TransportProtocol.Polling,
                        },
                        "85:0{\"sid\":\"LgtKYhIy1\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        new[]
                        {
                            "85:0{\"sid\":\"LgtKYhIy1\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "2:40",
                        }),
                    (
                        new SocketIOOptions
                        {
                            EIO = EngineIO.V3,
                            AutoUpgrade = true,
                            Transport = TransportProtocol.Polling,
                        },
                        "test2",
                        new[]
                        {
                            "74:0{\"sid\":\"LgtKYhIy2\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "2:40",
                        }),
                    (
                        new SocketIOOptions
                        {
                            EIO = EngineIO.V4,
                            AutoUpgrade = false,
                            Transport = TransportProtocol.Polling,
                        },
                        "0{\"sid\":\"LgtKYhIy3\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        new[]
                        {
                            "0{\"sid\":\"LgtKYhIy3\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "40{\"sid\":\"test\"}",
                        }),
                    (
                        new SocketIOOptions
                        {
                            EIO = EngineIO.V4,
                            AutoUpgrade = true,
                            Transport = TransportProtocol.Polling,
                        },
                        "test4",
                        new[]
                        {
                            "0{\"sid\":\"LgtKYhIy4\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                            "40{\"sid\":\"test\"}",
                        }),
                };
            }
        }

        [TestMethod]
        public async Task Should_throw_an_exception_when_server_is_unavailable_and_reconnection_is_false()
        {
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.GetStringAsync(It.IsAny<Uri>()))
                .ReturnsAsync("websocket");
            var mockWs = new Mock<IClientWebSocket>();
            mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Throws<System.Net.WebSockets.WebSocketException>();

            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                EIO = EngineIO.V3,
                Transport = TransportProtocol.Polling,
                Reconnection = false
            });
            io.HttpClientProvider = ()=> mockHttp.Object;
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io
                .Invoking(async x=>await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>()
                .WithMessage("Cannot connect to server '*'");
        }

        //[TestMethod]
        public async Task Reconnection_Should_Works()
        {
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                ReconnectionAttempts = 3
            });
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost:11002/socket.io/")
                .WithQueryString("EIO", "4")
                .WithQueryString("transport", "polling")
                .Respond("text/plain",
                    "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
            // io.HttpClient = mockHttp.ToHttpClient();

            var mockWs = new Mock<IClientWebSocket>();
            mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Throws<System.Net.WebSockets.WebSocketException>();
            io.ClientWebSocketProvider = () => mockWs.Object;

            int attempts = 0;
            io.OnReconnectAttempt += (s, att) => attempts = att;
            int failed = 0;
            io.OnReconnectFailed += (s, e) => failed++;
            int errorTimes = 0;
            io.OnReconnectError += (s, e) => errorTimes++;
            await io.ConnectAsync();

            mockWs.Verify(
                w => w.ConnectAsync(new Uri("ws://localhost:11002/socket.io/?EIO=4&transport=websocket"),
                    It.IsAny<CancellationToken>()), Times.Exactly(4));
            Assert.AreEqual(3, attempts);
            Assert.AreEqual(1, failed);
            Assert.AreEqual(3, errorTimes);
        }

        // [TestMethod]
        // public async Task Reconnection_Should_Works_After_First_Connection_Established()
        // {
        //     using var io = new SocketIO("http://localhost:11003");
        //     var mockHttp = new MockHttpMessageHandler();
        //     mockHttp.When("http://localhost:11003/socket.io/")
        //         .WithQueryString("EIO", "4")
        //         .WithQueryString("transport", "polling")
        //         .Respond("text/plain", "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
        //     io.HttpClient = mockHttp.ToHttpClient();

        //     var mockWs = new Mock<IClientWebSocket>();
        //     var textSubject = new Subject<string>();
        //     var bytesSubject = new Subject<byte[]>();
        //     mockWs.SetupGet(w => w.TextObservable).Returns(textSubject);
        //     mockWs.SetupGet(w => w.BytesObservable).Returns(bytesSubject);
        //     io.ClientWebSocketProvider = () => mockWs.Object;

        //     textSubject.OnNextLater("40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
        //     await io.ConnectAsync();
        //     Assert.IsTrue(io.Connected);
        //     textSubject.OnError(new Exception(nameof(Reconnection_Should_Works_After_First_Connection_Established)));
        //     Assert.IsFalse(io.Connected);
        //     mockWs.Verify(w => w.ConnectAsync(new Uri("ws://localhost:11003/socket.io/?EIO=4&transport=websocket"), It.IsAny<CancellationToken>()), Times.Exactly(2));
        // }

        // [TestMethod]
        // public async Task Disconnect_Should_Work()
        // {
        //     using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
        //     {
        //         Transport = TransportProtocol.WebSocket
        //     });

        //     var mockWs = new Mock<IClientWebSocket>();
        //     var textSubject = new Subject<string>();
        //     var bytesSubject = new Subject<byte[]>();
        //     mockWs.SetupGet(w => w.TextObservable).Returns(textSubject);
        //     mockWs.SetupGet(w => w.BytesObservable).Returns(bytesSubject);
        //     io.ClientWebSocketProvider = () => mockWs.Object;

        //     textSubject.OnNextLater("40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
        //     await io.ConnectAsync();
        //     Assert.IsTrue(io.Connected);

        //     await io.DisconnectAsync();
        //     Assert.IsFalse(io.Connected);
        //     mockWs.Verify(w => w.DisconnectAsync(CancellationToken.None), Times.Once());

        //     var p1 = Encoding.UTF8.GetBytes("41");
        //     mockWs.Verify(w => w.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, p1)), TransportMessageType.Text, true, CancellationToken.None), Times.Once());
        // }

        // [TestMethod]
        // public async Task AddExpectedException_Should_Work()
        // {
        //     using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
        //     {
        //         Transport = TransportProtocol.WebSocket,
        //         ReconnectionAttempts = 2
        //     });

        //     io.AddExpectedException(typeof(NotImplementedException));

        //     var mockWs = new Mock<IClientWebSocket>();
        //     mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Throws<NotImplementedException>();
        //     // var textSubject = new Subject<string>();
        //     // var bytesSubject = new Subject<byte[]>();
        //     // mockWs.SetupGet(w => w.TextObservable).Returns(textSubject);
        //     // mockWs.SetupGet(w => w.BytesObservable).Returns(bytesSubject);
        //     io.ClientWebSocketProvider = () => mockWs.Object;

        //     int failed = 0;
        //     io.OnReconnectFailed += (s, e) => failed++;
        //     await io.ConnectAsync();

        //     Assert.AreEqual(1, failed);
        // }

        // [TestMethod]
        // public async Task Eio4_WebSocket_Emit_Single_Bytes_Message()
        // {
        //     using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
        //     {
        //         Transport = TransportProtocol.WebSocket
        //     });

        //     var mockWs = new Mock<IClientWebSocket>();
        //     var textSubject = new Subject<string>();
        //     var bytesSubject = new Subject<byte[]>();
        //     mockWs.SetupGet(w => w.TextObservable).Returns(textSubject);
        //     mockWs.SetupGet(w => w.BytesObservable).Returns(bytesSubject);
        //     io.ClientWebSocketProvider = () => mockWs.Object;

        //     textSubject.OnNextLater("40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
        //     await io.ConnectAsync();

        //     var item1 = Encoding.UTF8.GetBytes("hello world 你好世界 hello world");
        //     await io.EmitAsync("1 param", item1);

        //     mockWs.Verify(w => w.ConnectAsync(new Uri("ws://localhost:11003/socket.io/?EIO=4&transport=websocket"), It.IsAny<CancellationToken>()), Times.Once());
        //     var p1 = Encoding.UTF8.GetBytes("451-[\"1 param\",{\"_placeholder\":true,\"num\":0}]");
        //     mockWs.Verify(w => w.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, p1)), TransportMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Once());
        //     mockWs.Verify(w => w.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, item1)), TransportMessageType.Binary, true, It.IsAny<CancellationToken>()), Times.Once());
        // }

        // [TestMethod]
        // public async Task WebSocket_Headers_Should_Be_Setted()
        // {
        //     using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
        //     {
        //         Transport = TransportProtocol.WebSocket,
        //         ExtraHeaders = new Dictionary<string, string>
        //         {
        //             ["h1"] = "v1"
        //         }
        //     });

        //     var mockWs = new Mock<IClientWebSocket>();
        //     var textSubject = new Subject<string>();
        //     var bytesSubject = new Subject<byte[]>();
        //     mockWs.SetupGet(w => w.TextObservable).Returns(textSubject);
        //     mockWs.SetupGet(w => w.BytesObservable).Returns(bytesSubject);
        //     io.ClientWebSocketProvider = () => mockWs.Object;

        //     textSubject.OnNextLater("40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
        //     await io.ConnectAsync();

        //     mockWs.Verify(w => w.ConnectAsync(new Uri("ws://localhost:11003/socket.io/?EIO=4&transport=websocket"), It.IsAny<CancellationToken>()), Times.Once());
        //     mockWs.Verify(w => w.AddHeader("h1", "v1"), Times.Once());
        // }

        //[TestMethod]
        //public async Task Eio3_Polling_Emit_Single_Bytes_Message()
        //{
        //    using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
        //    {
        //        Transport = TransportProtocol.Polling,
        //        AutoUpgrade = false,
        //        EIO = EngineIO.V3
        //    });

        //    int times = 0;
        //    int placeholder = 0;
        //    var mockHttp = new MockHttpMessageHandler();
        //    mockHttp.When("http://localhost:11002/socket.io/")
        //        .WithQueryString("EIO", "3")
        //        .WithQueryString("transport", "polling")
        //        .Respond(reqMsg =>
        //        {
        //            try
        //            {
        //                switch (times)
        //                {
        //                    case 0:
        //                        return new StringContent("85:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAA\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}", Encoding.UTF8, "text/plain");
        //                    case 1:
        //                        return new StringContent("2:40", Encoding.UTF8, "text/plain");
        //                    default:
        //                        if (reqMsg.Method == HttpMethod.Post)
        //                        {
        //                            if (reqMsg.Content != null)
        //                            {
        //                                string content = reqMsg.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        //                                if (content == "451-[\"1 param\",{\"_placeholder\":true,\"num\":0}]")
        //                                    placeholder++;
        //                                else if(content == "")
        //                            }
        //                        }
        //                        return new StringContent(string.Empty, Encoding.UTF8, "text/plain");
        //                        //Task.Delay(1000).Wait();
        //                        //return new StringContent(string.Empty, Encoding.UTF8, "text/plain");
        //                }
        //            }
        //            finally
        //            {
        //                times++;
        //            }
        //        });
        //    io.HttpClient = mockHttp.ToHttpClient();

        //    await io.ConnectAsync();
        //    //  textSubject.OnNext("40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");

        //    var item1 = Encoding.UTF8.GetBytes("hello world 你好世界 hello world");
        //    await io.EmitAsync("1 param", item1);
        //    //mockWs.Verify(w => w.ConnectAsync(new Uri("ws://localhost:11003/socket.io/?EIO=4&transport=websocket"), It.IsAny<CancellationToken>()), Times.Once());
        //    //var p1 = Encoding.UTF8.GetBytes("451-[\"1 param\",{\"_placeholder\":true,\"num\":0}]");
        //    // mockWs.Verify(w => w.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, p1)), TransportMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Once());
        //    // mockWs.Verify(w => w.SendAsync(It.Is<byte[]>(p => Enumerable.SequenceEqual(p, item1)), TransportMessageType.Binary, true, It.IsAny<CancellationToken>()), Times.Once());
        //}
    }
}