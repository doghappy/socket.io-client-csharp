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
using SocketIOClient.Transport.WebSockets;
using Range = Moq.Range;

namespace SocketIOClient.UnitTests.Transport.Http
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
                    It.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get
                                                     && Uri.Compare(
                                                         req.RequestUri,
                                                         uri,
                                                         UriComponents.AbsoluteUri,
                                                         UriFormat.SafeUnescaped,
                                                         StringComparison.OrdinalIgnoreCase) == 0),
                    It.IsAny<CancellationToken>()))
                .Throws<Exception>();

            using var transport = new HttpTransport(options, mockedHandler.Object);

            using var cts = new CancellationTokenSource(100);
            var token = cts.Token;
            await transport
                .Invoking(async x => await x.ConnectAsync(uri, token))
                .Should()
                .ThrowAsync<TransportException>()
                .WithMessage("Could not connect to '*'");
        }

        [TestMethod]
        [DataRow(EngineIO.V4, 1000, 8, 12)]
        [DataRow(EngineIO.V3, 1000, 8, 12)]
        public async Task Polling_ShouldWork(EngineIO eio, int delay, int min, int max)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.Setup(h => h.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .Returns(async () => await Task.Delay(100));
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            _ = new HttpTransport(new TransportOptions
            {
                EIO = eio,
            }, mockHttpPollingHandler.Object);
            await mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            await Task.Delay(delay);

            mockHttpPollingHandler.Verify(e => e.GetAsync("&sid=LgtKYhIy7tUzKHH9AAAB", CancellationToken.None),
                Times.Between(min, max, Range.Inclusive));
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

        private static IEnumerable<object[]> OnTextReceivedCases => OnTextReceivedTupleCases.Select(x => new [] { x.eio, x.text, x.expected });

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
                            Upgrades = new[] { "websocket" },
                            PingInterval = 10000,
                            PingTimeout = 5000,
                        }),
                };
            }
        }

        [TestMethod]
        [DataRow("{\"name\":\"admin\"}", null, "40{\"name\":\"admin\"}")]
        [DataRow("{\"token\":\"123\"}", "/test", "40/test,{\"token\":\"123\"}")]
        public void ShouldSendAuth_WhenConnecting(string auth, string ns, string expected)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            _ = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
                Auth = auth,
            }, mockHttpPollingHandler.Object)
            {
                Namespace = ns,
            };
            mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");

            mockHttpPollingHandler.Verify(
                h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", expected, CancellationToken.None),
                Times.Once());
        }

        [TestMethod]
        // [DataRow(100, 100, 1, 1)]
        [DataRow(1000, 100, 5, 15)]
        public async Task Eio3_Ping_ShouldWork(int delay, int pingInterval, int min, int max)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            _ = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
            }, mockHttpPollingHandler.Object);
            await mockHttpPollingHandler.Object.OnTextReceived($"0{{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":{pingInterval},\"pingTimeout\":5000}}");
            await mockHttpPollingHandler.Object.OnTextReceived("40");
            await Task.Delay(delay);

            using var cts = new CancellationTokenSource(5000);
            mockHttpPollingHandler.Verify(h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", "2", It.IsNotIn(CancellationToken.None)),
                Times.Between(min, max, Range.Inclusive));
        }

        [TestMethod]
        [DynamicData(nameof(Eio3NamespaceQueryCases))]
        public async Task Eio3_NamespaceQuery(string ns, IEnumerable<KeyValuePair<string, string>> query, string expected)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
                Query = query,
            }, mockHttpPollingHandler.Object);
            transport.Namespace = ns;
            await mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":1000,\"pingTimeout\":5000}");
            mockHttpPollingHandler.Verify(
                h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", expected, CancellationToken.None),
                Times.Once());
        }

        private static IEnumerable<object[]> Eio3NamespaceQueryCases => Eio3NamespaceQueryTupleCases.Select(x => new object[] { x.ns, x.query, x.expected });

        private static IEnumerable<(string ns, IEnumerable<KeyValuePair<string, string>> query, string expected)> Eio3NamespaceQueryTupleCases
        {
            get
            {
                return new (string ns, IEnumerable<KeyValuePair<string, string>> query, string expected)[]
                {
                    (
                        "/nsp",
                        null,
                        "40/nsp,"),
                    (
                        "/hello",
                        new[]
                        {
                            new KeyValuePair<string, string>("hello", "world"),
                        },
                        "40/hello?hello=world,"),
                    (
                        "/nsp",
                        new[]
                        {
                            new KeyValuePair<string, string>("user", "tom"),
                            new KeyValuePair<string, string>("token", "123"),
                        },
                        "40/nsp?user=tom&token=123,"),
                };
            }
        }

        [TestMethod]
        [DataRow(null, null, "40")]
        [DataRow("", null, "40")]
        [DataRow(null, "", "40")]
        [DataRow("/nsp", "", "40/nsp,")]
        [DataRow("/hello", "{\"hello\":\"world\"}", "40/hello,{\"hello\":\"world\"}")]
        [DataRow("/nsp", "{\"user\":\"tom\",\"token\":\"123\"}", "40/nsp,{\"user\":\"tom\",\"token\":\"123\"}")]
        public async Task Eio4_NamespaceAuth(string ns, string auth, string expected)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
                Auth = auth,
            }, mockHttpPollingHandler.Object);
            transport.Namespace = ns;
            await mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":1000,\"pingTimeout\":5000}");
            mockHttpPollingHandler.Verify(
                h => h.PostAsync("&sid=LgtKYhIy7tUzKHH9AAAB", expected, CancellationToken.None),
                Times.Once());
        }

        [TestMethod]
        [DynamicData(nameof(OnBinaryMessagesReceivedCases))]
        public void OnBinaryMessagesReceived(EngineIO eio, IEnumerable<(bool IsText, object Data)> input, IEnumerable<object> output)
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
            mockHttpPollingHandler.SetupProperty(h => h.OnBytesReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = eio,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = (msg => msgs.Add(msg));

            foreach (var item in input)
            {
                if (item.IsText)
                {
                    mockHttpPollingHandler.Object.OnTextReceived((string)item.Data);
                }
                else
                {
                    mockHttpPollingHandler.Object.OnBytesReceived((byte[])item.Data);
                }
            }

            msgs.Should().BeEquivalentTo(output);
        }

        private static IEnumerable<object[]> OnBinaryMessagesReceivedCases => OnBinaryMessagesReceivedTupleCases.Select(x => new object[] { x.eio, x.input, x.output });

        private static IEnumerable<(EngineIO eio, IEnumerable<(bool IsText, object Data)> input, IEnumerable<object> output)> OnBinaryMessagesReceivedTupleCases
        {
            get
            {
                return new (EngineIO eio, IEnumerable<(bool IsText, object Data)> input, IEnumerable<object> output)[]
                {
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, null),
                        },
                        Array.Empty<object>()),
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, "test"),
                        },
                        Array.Empty<object>()),
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, "451-[\"test\"]"),
                        },
                        Array.Empty<object>()),
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, "451-[\"test\"]"),
                            (true, "451-[\"test\"]"),
                        },
                        Array.Empty<object>()),
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, "451-[\"test\"]"),
                            (false, new byte[] { }),
                        },
                        new object[]
                        {
                            new
                            {
                                Type = MessageType.BinaryMessage,
                                BinaryCount = 1,
                                EIO = EngineIO.V3,
                                Event = "test",
                            },
                        }),
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, "452-[\"test\"]"),
                            (false, new byte[] { }),
                        },
                        Array.Empty<object>()),
                    (
                        EngineIO.V3,
                        new (bool IsText, object Data)[]
                        {
                            (true, "452-[\"test\"]"),
                            (false, new byte[] { }),
                            (false, new byte[] { }),
                        },
                        new object[]
                        {
                            new
                            {
                                Type = MessageType.BinaryMessage,
                                BinaryCount = 2,
                                EIO = EngineIO.V3,
                                Event = "test",
                            },
                        }),
                    (
                        EngineIO.V4,
                        new (bool IsText, object Data)[]
                        {
                            (true, "452-[\"test2\"]"),
                            (true, "451-[\"test1\"]"),
                            (false, new byte[] { }),
                            (false, new byte[] { }),
                        },
                        new object[]
                        {
                            new
                            {
                                Type = MessageType.BinaryMessage,
                                BinaryCount = 2,
                                EIO = EngineIO.V4,
                                Event = "test2",
                            },
                        }),
                    (
                        EngineIO.V4,
                        new (bool IsText, object Data)[]
                        {
                            (true, "452-[\"test2\"]"),
                            (true, "451-[\"test1\"]"),
                            (false, new byte[] { }),
                            (false, new byte[] { }),
                            (false, new byte[] { }),
                        },
                        new object[]
                        {
                            new
                            {
                                Type = MessageType.BinaryMessage,
                                BinaryCount = 2,
                                EIO = EngineIO.V4,
                                Event = "test2",
                            },
                            new
                            {
                                Type = MessageType.BinaryMessage,
                                BinaryCount = 1,
                                EIO = EngineIO.V4,
                                Event = "test1",
                            },
                        }),
                };
            }
        }

        [TestMethod]
        public void PingPongTest()
        {
            var msgs = new List<IMessage>();
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V4,
            }, mockHttpPollingHandler.Object);
            transport.OnReceived = m => msgs.Add(m);

            mockHttpPollingHandler.Object.OnTextReceived("2");

            mockHttpPollingHandler.Verify(
                h => h.PostAsync(null, "3", CancellationToken.None),
                Times.Once());
            msgs.Should()
                .BeEquivalentTo(new object[]
                {
                    new
                    {
                        Type = MessageType.Ping,
                        Protocol = TransportProtocol.Polling,
                        EIO = EngineIO.V4,
                    },
                    new
                    {
                        Type = MessageType.Pong,
                        Protocol = TransportProtocol.Polling,
                        EIO = EngineIO.V4,
                    },
                });

            var pong = msgs[1] as PongMessage;
            pong.Duration.Should()
                .BeGreaterThan(TimeSpan.Zero)
                .And.BeLessThan(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        [DynamicData(nameof(SendCases))]
        public async Task Send(EngineIO eio, Payload payload, int textTimes, int byteTimes)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = eio,
            }, mockHttpPollingHandler.Object);

            await transport.SendAsync(payload, CancellationToken.None);

            mockHttpPollingHandler.Verify(
                h => h.PostAsync(null, payload.Text, CancellationToken.None),
                Times.Exactly(textTimes));
            mockHttpPollingHandler.Verify(
                h => h.PostAsync(null, It.IsAny<IEnumerable<byte[]>>(), CancellationToken.None),
                Times.Exactly(byteTimes));
        }

        private static IEnumerable<object[]> SendCases => SendTupleCases.Select(x => new object[] { x.eio, x.payload, x.textTimes, x.byteTimes });

        private static IEnumerable<(EngineIO eio, Payload payload, int textTimes, int byteTimes)> SendTupleCases
        {
            get
            {
                return new (EngineIO eio, Payload payload, int textTimes, int byteTimes)[]
                {
                    (EngineIO.V3, new Payload(), 0, 0),
                    (EngineIO.V3, new Payload { Text = string.Empty }, 0, 0),
                    (EngineIO.V4, new Payload { Text = "hello word" }, 1, 0),
                    (EngineIO.V4, new Payload { Bytes = new List<byte[]>() }, 0, 0),
                    (EngineIO.V4, new Payload { Bytes = new List<byte[]> { new byte[] { } } }, 0, 1),
                };
            }
        }
        
        [TestMethod]
        [DataRow(EngineIO.V3, 100)]
        [DataRow(EngineIO.V4, 100)]
        public void ConcurrentlySend(EngineIO eio, int times)
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            var i = 0;
            mockHttpPollingHandler
                .Setup(h => h.PostAsync(null, It.IsAny<string>(), CancellationToken.None))
                .Callback(() => (++i % 2).Should().Be(1));
            mockHttpPollingHandler
                .Setup(h => h.PostAsync(null, It.IsAny<IEnumerable<byte[]>>(), CancellationToken.None))
                .Callback(() => (++i % 2).Should().Be(0));

            var transport = new HttpTransport(new TransportOptions
            {
                EIO = eio,
            }, mockHttpPollingHandler.Object);

            var payload = new Payload
            {
                Text = new string('a', ChunkSize.Size8K + 1),
                Bytes = new List<byte[]>
                {
                    new byte[ChunkSize.Size8K + 1],
                },
            };

            Parallel.For(0, times, _ => transport.SendAsync(payload, CancellationToken.None).GetAwaiter().GetResult());
            
            mockHttpPollingHandler.Verify(
                h => h.PostAsync(null, It.IsAny<string>(), CancellationToken.None),
                Times.Exactly(times));
            mockHttpPollingHandler.Verify(
                h => h.PostAsync(null, It.IsAny<IEnumerable<byte[]>>(), CancellationToken.None),
                Times.Exactly(times));
        }

        [TestMethod]
        public async Task OpenedMessage_should_be_in_front_of_ConnectedMessage()
        {
            var mockHttpPollingHandler = new Mock<IHttpPollingHandler>();
            mockHttpPollingHandler.SetupProperty(h => h.OnTextReceived);
            var transport = new HttpTransport(new TransportOptions
            {
                EIO = EngineIO.V3,
                ConnectionTimeout = TimeSpan.FromSeconds(1),
            }, mockHttpPollingHandler.Object);
            var msgs = new List<IMessage>();
            transport.OnReceived = m => msgs.Add(m);

            var connectedTask = mockHttpPollingHandler.Object.OnTextReceived("40");
            await Task.Delay(100);
            _ = mockHttpPollingHandler.Object.OnTextReceived(
                "0{\"sid\":\"test\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
            await connectedTask;

            msgs.Should().BeEquivalentTo(new object[]
            {
                new
                {
                    Type = MessageType.Opened,
                    Sid = "test",
                    Upgrades = new[] { "websocket" },
                    PingInterval = 10000,
                    PingTimeout = 5000,
                },
                new
                {
                    Type = MessageType.Connected,
                },
            }, options => options.WithStrictOrdering());
        }
    }
}