using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTests
{
    [TestClass]
    public class SocketIOTest
    {
        [TestMethod]
        [DynamicData(nameof(ConnectCases))]
        public async Task Should_be_able_to_connect(SocketIOOptions options, string[] messages)
        {
            int i = -1;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GetResponseMessage(ref i, messages));
            var mockWs = new Mock<IClientWebSocket>();
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() => GetWebSocketResult(ref i, messages));
            using var io = new SocketIO("http://localhost:11002", options);
            io.HttpClient = mockHttp.Object;
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

        private static readonly Regex wsDelayMessageRegex = new Regex(@"^Task\.Delay\((?<ms>\d+)\);(?<text>.*)");

        private static string GetResponseText(ref int i, string[] messages)
        {
            i++;
            if (i < messages.Length)
            {
                string text = messages[i];
                var result = wsDelayMessageRegex.Match(text);
                if (result.Success)
                {
                    var s = int.Parse(result.Groups["ms"].Value);
                    Task.Delay(s).Wait();
                    return result.Groups["text"].Value;
                }

                return text;
            }

            Task.Delay(1000).Wait();
            return string.Empty;
        }

        private static HttpResponseMessage GetResponseMessage(ref int i, string[] messages)
        {
            string text = GetResponseText(ref i, messages);
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(text, new MediaTypeHeaderValue("text/pain")),
            };
        }

        private static WebSocketReceiveResult GetWebSocketResult(ref int i, string[] messages)
        {
            var text = GetResponseText(ref i, messages);
            return NewWebSocketResult(text);
        }

        private static WebSocketReceiveResult NewWebSocketResult(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return new WebSocketReceiveResult
            {
                MessageType = TransportMessageType.Text,
                Buffer = bytes,
                EndOfMessage = true,
                Count = bytes.Length,
            };
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
                .ReturnsAsync(() => GetWebSocketResult(ref i, messages));
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                AutoUpgrade = true,
                Transport = TransportProtocol.Polling,
                EIO = eio,
            });
            io.HttpClient = mockHttp.Object;
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
                .ReturnsAsync(() => GetResponseMessage(ref i, messages));
            using var io = new SocketIO("http://localhost:11002", options);
            io.HttpClient = mockHttp.Object;

            await io.ConnectAsync();

            io.Connected.Should().BeTrue();
            io.Options.Transport.Should().Be(TransportProtocol.Polling);
        }

        private static IEnumerable<object[]> NotUpgradeToWebSocketCases =>
            NotUpgradeToWebSocketTupleCases.Select(x => new object[] { x.options, x.handshake, x.messages });

        private static IEnumerable<(SocketIOOptions options, string handshake, string[] messages)>
            NotUpgradeToWebSocketTupleCases
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
            io.HttpClient = mockHttp.Object;
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io
                .Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>()
                .WithMessage("Cannot connect to server '*'");
        }

        [TestMethod]
        [DataRow(3)]
        public async Task Reconnecting_events_should_be_work(int attempts)
        {
            using var io = new SocketIO("http://localhost:11002", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                ReconnectionAttempts = attempts,
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
            });
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.GetStringAsync(It.IsAny<Uri>()))
                .ReturnsAsync("websocket");
            io.HttpClient = mockHttp.Object;

            var mockWs = new Mock<IClientWebSocket>();
            mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Throws<System.Net.WebSockets.WebSocketException>();
            io.ClientWebSocketProvider = () => mockWs.Object;

            int attemptTimes = 0;
            io.OnReconnectAttempt += (s, att) => attemptTimes = att;
            int failedTimes = 0;
            io.OnReconnectFailed += (s, e) => failedTimes++;
            int errorTimes = 0;
            io.OnReconnectError += (s, e) => errorTimes++;
            await io
                .Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>()
                .WithMessage("Cannot connect to server *");

            mockWs.Verify(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()),
                Times.Exactly(attempts + 1));

            attemptTimes.Should().Be(attempts);
            errorTimes.Should().Be(attempts + 1);
            failedTimes.Should().Be(1);
        }

        [TestMethod]
        public async Task Should_be_able_to_reconnect_after_lost_connection()
        {
            using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
            });
            int i = -1;
            var messages = new[]
            {
                "40{\"sid\":\"1\"}",
            };
            var mockWs = new Mock<IClientWebSocket>();
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() => GetWebSocketResult(ref i, messages));
            io.ClientWebSocketProvider = () =>
            {
                i = -1;
                return mockWs.Object;
            };

            await io.ConnectAsync();
            io.Connected.Should().BeTrue();
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ThrowsAsync(new Exception("Cannot receive messages"));
            await Task.Delay(1000);
            io.Connected.Should().BeFalse();
            mockWs.Verify(
                w => w.ConnectAsync(new Uri("ws://localhost:11003/socket.io/?EIO=4&transport=websocket"),
                    It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Should_be_able_to_disconnect()
        {
            using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
            });

            int i = -1;
            var messages = new[]
            {
                "40{\"sid\":\"1\"}",
            };
            var mockWs = new Mock<IClientWebSocket>();
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() => GetWebSocketResult(ref i, messages));
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io.ConnectAsync();
            io.Connected.Should().BeTrue();

            await io.DisconnectAsync();
            io.Connected.Should().BeFalse();

            mockWs.Verify(w => w.DisconnectAsync(CancellationToken.None), Times.Once());

            var p1 = "41"u8.ToArray();
            mockWs.Verify(
                w => w.SendAsync(It.Is<byte[]>(p => p.SequenceEqual(p1)), TransportMessageType.Text, true,
                    CancellationToken.None), Times.Once());
        }

        [TestMethod]
        public async Task Should_be_able_to_add_expected_exception()
        {
            using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                ReconnectionAttempts = 2,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
            });

            io.AddExpectedException(typeof(NotImplementedException));

            var mockWs = new Mock<IClientWebSocket>();
            mockWs.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Throws<NotImplementedException>();
            io.ClientWebSocketProvider = () => mockWs.Object;

            int failed = 0;
            int attempt = 0;
            int error = 0;
            io.OnReconnectFailed += (s, e) => failed++;
            io.OnReconnectAttempt += (s, e) => attempt++;
            io.OnReconnectError += (s, e) => error++;
            await io
                .Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>()
                .WithMessage("Cannot connect to server *");

            failed.Should().Be(1);
            attempt.Should().Be(2);
            error.Should().Be(3);
        }

        [TestMethod]
        [DynamicData(nameof(EmitCases))]
        public async Task Should_be_able_to_emit(string eventName,
            object[] data,
            Func<IMessage, bool> checkPoints)
        {
            using var io = new SocketIO("http://localhost:11003");
            var mockTransport = new Mock<ITransport>();
            io.Transport = mockTransport.Object;

            await io.EmitAsync(eventName, data);

            mockTransport.Verify(x => x.SendAsync(It.Is<IMessage>(m => checkPoints(m)), It.IsAny<CancellationToken>()),
                Times.Once());
        }

        private static IEnumerable<object[]> EmitCases =>
            EmitTupleCases.Select(x => new object[] { x.name, x.data, x.checkPoints });

        private static IEnumerable<(string name, object[] data, Func<IMessage, bool> checkPoints)>
            EmitTupleCases
        {
            get
            {
                return new (string name, object[] data, Func<IMessage, bool> checkPoints)[]
                {
                    (
                        "1 param",
                        new object[]
                        {
                            "hello world 你好🌍🌏🌎 hello world",
                        },
                        m =>
                        {
                            var msg = (EventMessage)m;
                            var text = System.Text.Json.JsonSerializer.Serialize(
                                "hello world 你好🌍🌏🌎 hello world");
                            return msg.Event == "1 param" && msg.Json == $"[{text}]";
                        }),
                    (
                        "test-2",
                        new object[]
                        {
                            "hello world 你好🌍🌏🌎 hello world"u8.ToArray(),
                        },
                        m =>
                        {
                            var msg = (BinaryMessage)m;
                            return msg.Event == "test-2"
                                   && msg.Json == "[{\"_placeholder\":true,\"num\":0}]"
                                   && msg.OutgoingBytes[0]
                                       .SequenceEqual("hello world 你好🌍🌏🌎 hello world"u8.ToArray());
                        }),
                };
            }
        }

        [TestMethod]
        public async Task Should_be_able_to_add_header()
        {
            using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
                ExtraHeaders = new Dictionary<string, string>
                {
                    ["h1"] = "v1"
                }
            });

            int i = -1;
            var messages = new[]
            {
                "40{\"sid\":\"1\"}",
            };
            var mockWs = new Mock<IClientWebSocket>();
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() => GetWebSocketResult(ref i, messages));
            io.ClientWebSocketProvider = () => mockWs.Object;

            await io.ConnectAsync();

            mockWs.Verify(w => w.AddHeader("h1", "v1"), Times.Once());
        }

        [TestMethod]
        [DataRow("http://localhost:11002", null)]
        [DataRow("http://localhost:11002/", null)]
        [DataRow("http://localhost:11002/namespace", "/namespace")]
        [DataRow("http://localhost:11002/namespace/test", "/namespace/test")]
        public void Should_set_namespace(string url, string ns)
        {
            using var io = new SocketIO(url);
            io.Namespace.Should().Be(ns);
        }

        [TestMethod]
        public async Task Headers_should_be_added_when_handshaking_ws()
        {
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.GetStringAsync(It.IsAny<Uri>()))
                .ReturnsAsync("websocket");
            var messages = new[]
            {
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}"
            };
            var mockWs = new Mock<IClientWebSocket>();
            int i = -1;
            mockWs
                .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
            mockWs
                .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
                .ReturnsAsync(() => GetWebSocketResult(ref i, messages));
            using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
            {
                ConnectionTimeout = TimeSpan.FromSeconds(1),
                ExtraHeaders = new Dictionary<string, string>
                {
                    ["name"] = "value"
                }
            })
            {
                HttpClient = mockHttp.Object,
                ClientWebSocketProvider = () => mockWs.Object,
            };
            await io.ConnectAsync();
            
            mockHttp.Verify(m=>m.AddHeader("name", "value"), Times.Once());
        }
        
        [TestMethod]
        public async Task Headers_should_be_added_when_handshaking_http()
        {
            var messages = new[]
            {
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}"
            };
            int i = -1;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GetResponseMessage(ref i, messages));
            using var io = new SocketIO("http://localhost:11003", new SocketIOOptions
            {
                AutoUpgrade = false,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
                ExtraHeaders = new Dictionary<string, string>
                {
                    ["name"] = "value"
                }
            })
            {
                HttpClient = mockHttp.Object,
            };
            await io.ConnectAsync();
            
            mockHttp.Verify(m=>m.AddHeader("name", "value"), Times.Once());
        }
    }
}