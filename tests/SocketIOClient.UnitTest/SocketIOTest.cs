using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using RichardSzalay.MockHttp;
using Moq;
using System.Threading;
using System.Net.WebSockets;
using System.Net.Http;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTest
{
    [TestClass]
    public class SocketIOTest
    {
        //[TestMethod]
        public async Task Eio3_Polling_Should_Connect_Successful()
        {
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                EIO = EngineIO.V3,
                Transport = TransportProtocol.Polling
            });
            int times = 0;
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost:11002/socket.io/")
                .WithQueryString("EIO", "3")
                .WithQueryString("transport", "polling")
                .Respond(reqMsg =>
                {
                    try
                    {
                        switch (times)
                        {
                            case 0:
                            case 1:
                                return new StringContent("85:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}", Encoding.UTF8, "text/plain");
                            case 2:
                                return new StringContent("2:40", Encoding.UTF8, "text/plain");
                            default:
                                Task.Delay(10000).Wait();
                                return new StringContent("1:2", Encoding.UTF8, "text/plain");
                        }
                    }
                    finally
                    {
                        times++;
                    }
                });
            io.HttpClient = mockHttp.ToHttpClient();
            //string text1 =await io.HttpClient.GetStringAsync("http://localhost:11002/socket.io/?EIO=3&transport=polling");
            //string text2 =await io.HttpClient.GetStringAsync("http://localhost:11002/socket.io/?EIO=3&transport=polling&sid=LgtKYhIy7tUzKHH9AAAB&t=123");
            //string text3 =await io.HttpClient.GetStringAsync("http://localhost:11002/socket.io/?EIO=3&transport=polling&sid=LgtKYhIy7tUzKHH9AAAB&t=123");

            await io.ConnectAsync();

            Assert.IsTrue(io.Connected);
        }

        // [TestMethod]
        // public async Task Eio3_Polling_Should_Upgrade_To_WebSocket_Successful()
        // {
        //     using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
        //     {
        //         EIO = EngineIO.V3,
        //         Transport = TransportProtocol.Polling
        //     });
        //     var mockHttp = new MockHttpMessageHandler();
        //     mockHttp.When("http://localhost:11002/socket.io/")
        //         .WithQueryString("EIO", "3")
        //         .WithQueryString("transport", "polling")
        //         .Respond("text/plain", "96:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
        //     io.HttpClient = mockHttp.ToHttpClient();

        //     var mockWs = new Mock<IClientWebSocket>();
        //     io.ClientWebSocketProvider = () => mockWs.Object;

        //     textSubject.OnNextLater(new[]
        //     {
        //         "0{\"sid\":\"CTWaM0_v5bx3C0S3AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
        //         "40"
        //     }, 2000);
        //     await io.ConnectAsync();

        //     mockWs.Verify(w => w.ConnectAsync(new Uri("ws://localhost:11002/socket.io/?EIO=3&transport=websocket"), It.IsAny<CancellationToken>()), Times.Once());
        //     Assert.AreEqual(TransportProtocol.WebSocket, io.Options.Transport);
        //     Assert.IsTrue(io.Connected);
        //     Assert.AreEqual(io.Id, "CTWaM0_v5bx3C0S3AAAB");
        // }

        //[TestMethod]
        public async Task Should_Not_Upgrade_To_WebSocket()
        {
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                EIO = EngineIO.V3,
                AutoUpgrade = false,
                Transport = TransportProtocol.Polling
            });
            int times = 0;
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost:11002/socket.io/")
                .WithQueryString("EIO", "3")
                .WithQueryString("transport", "polling")
                .Respond(reqMsg =>
                {
                    try
                    {
                        if (times == 0)
                            return new StringContent("85:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAA\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}", Encoding.UTF8, "text/plain");
                        else if (times == 1)
                            return new StringContent("2:40", Encoding.UTF8, "text/plain");
                        else
                        {
                            Task.Delay(1000).Wait();
                            return new StringContent(string.Empty, Encoding.UTF8, "text/plain");
                        }
                    }
                    finally
                    {
                        times++;
                    }
                });
            io.HttpClient = mockHttp.ToHttpClient();

            await io.ConnectAsync();

            Assert.AreEqual(TransportProtocol.Polling, io.Options.Transport);
            Assert.IsTrue(io.Connected);
        }

        [TestMethod]
        [ExpectedException(typeof(ConnectionException))]
        public async Task Should_Throw_An_Exception_If_Reconnection_Is_False_And_Server_Unavailable()
        {
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                EIO = EngineIO.V3,
                Transport = TransportProtocol.Polling,
                Reconnection = false
            });
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When("http://localhost:11002/socket.io/")
                .WithQueryString("EIO", "3")
                .WithQueryString("transport", "polling")
                .Respond("text/plain", "96:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
            io.HttpClient = mockHttp.ToHttpClient();

            var mockWs = new Mock<IClientWebSocket>();
            mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Throws<WebSocketException>();
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io.ConnectAsync();
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
                .Respond("text/plain", "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
            io.HttpClient = mockHttp.ToHttpClient();

            var mockWs = new Mock<IClientWebSocket>();
            mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Throws<WebSocketException>();
            io.ClientWebSocketProvider = () => mockWs.Object;

            int attempts = 0;
            io.OnReconnectAttempt += (s, att) => attempts = att;
            int failed = 0;
            io.OnReconnectFailed += (s, e) => failed++;
            int errorTimes = 0;
            io.OnReconnectError += (s, e) => errorTimes++;
            await io.ConnectAsync();

            mockWs.Verify(w => w.ConnectAsync(new Uri("ws://localhost:11002/socket.io/?EIO=4&transport=websocket"), It.IsAny<CancellationToken>()), Times.Exactly(4));
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