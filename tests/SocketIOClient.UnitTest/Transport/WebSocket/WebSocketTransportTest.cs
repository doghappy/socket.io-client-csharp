using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTest.Transport.WebSocket;

[TestClass]
public class WebSocketTransportTest
{
    [TestMethod]
    public async Task Connect_ThrowException_IfDirty()
    {
        Uri uri = new("http://localhost:11002/socket.io/?EIO=3&transport=websocket");
        var mockWs = new Mock<IClientWebSocket>();

        var transport = new WebSocketTransport(new TransportOptions
        {
            EIO = EngineIO.V3,
        }, mockWs.Object);

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
        Uri uri = new("http://localhost:11002/socket.io/?EIO=3&transport=websocket");
        var mockWs = new Mock<IClientWebSocket>();
        mockWs
            .Setup(m => m.ConnectAsync(uri, It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        var transport = new WebSocketTransport(new TransportOptions
        {
            EIO = EngineIO.V3,
        }, mockWs.Object);

        using var cts = new CancellationTokenSource(100);
        await transport
            .Invoking(async x => await x.ConnectAsync(uri, cts.Token))
            .Should()
            .ThrowAsync<TransportException>()
            .WithMessage("Could not connect to '*'");
    }

    [TestMethod]
    [DynamicData(nameof(OnReceivedCases))]
    public async Task OnReceived(EngineIO eio, List<(TransportMessageType type, byte[] data)> items, object expectedMsg, IEnumerable<string> expectedElements, IEnumerable<byte[]> expectedBytes)
    {
        IMessage msg = null;
        int i = 0;
        var faker = new ReceiveAsyncFaker(eio);
        var mockWs = new Mock<IClientWebSocket>();
        mockWs
            .Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback(() => mockWs.SetupGet(w => w.State).Returns(WebSocketState.Open));
        mockWs
            .Setup(w => w.ReceiveAsync(It.IsAny<int>(), It.IsNotIn(CancellationToken.None)))
            .Returns(async () =>
            {
                if (i < items.Count)
                {
                    var wrr = await faker.ReceiveAsync(items[i].type, items[i].data, () => Task.CompletedTask);
                    if (wrr.EndOfMessage)
                    {
                        i++;
                    }
                    return wrr;
                }
                else
                {
                    await Task.Delay(int.MaxValue);
                    return null;
                }
            });

        var transport = new WebSocketTransport(new TransportOptions
        {
            EIO = eio,
        }, mockWs.Object);
        transport.OnReceived = (m => msg = m);
        await transport.ConnectAsync(null, CancellationToken.None);
        await Task.Delay(100);

        msg.Should().BeEquivalentTo(expectedMsg);
        if (msg is IJsonMessage jsonMsg)
        {
            jsonMsg.JsonElements
                .Select(x => x.GetRawText())
                .Should()
                .Equal(expectedElements);
        }
        msg.IncomingBytes
            .Should()
            .BeEquivalentTo(expectedBytes, options => options.WithStrictOrdering());
    }

    private static IEnumerable<object[]> OnReceivedCases => OnReceivedTupleCases.Select(x => new object[] { x.eio, x.items, x.expectedMsg, x.expectedElements, x.expectedBytes });

    private static IEnumerable<(EngineIO eio, List<(TransportMessageType type, byte[] data)> items, object expectedMsg, IEnumerable<string> expectedElements, IEnumerable<byte[]> expectedBytes)> OnReceivedTupleCases
    {
        get
        {
            return new (EngineIO eio, List<(TransportMessageType type, byte[] data)> items, object expectedMsg, IEnumerable<string> expectedElements, IEnumerable<byte[]> expectedBytes)[]
            {
                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes("0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}")),
                    },
                    new
                    {
                        Type = MessageType.Opened,
                        EIO = EngineIO.V3,
                        Sid = "LgtKYhIy7tUzKHH9AAAB",
                        Upgrades = new[] { "websocket" },
                        PingInterval = 10000,
                        PingTimeout = 5000,
                    },
                    null,
                    null),
                (
                    EngineIO.V4,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes("42[\"hello\",\"world\"]")),
                    },
                    new
                    {
                        Type = MessageType.EventMessage,
                        EIO = EngineIO.V4,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 0,
                    },
                    new[]
                    {
                        "\"world\"",
                    },
                    null),
                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes($"42[\"hello\",\"{new string('a', ChunkSize.Size8K)}\"]")),
                    },
                    new
                    {
                        Type = MessageType.EventMessage,
                        EIO = EngineIO.V3,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 0,
                    },
                    new[]
                    {
                        $"\"{new string('a', ChunkSize.Size8K)}\"",
                    },
                    null),
                (
                    EngineIO.V4,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes("451-[\"hello\",{\"_placeholder\":true,\"num\":0}]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V4,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 1,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K).ToArray(),
                    }),
                (
                    EngineIO.V4,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes($"451-[\"hello\",{{\"_placeholder\":true,\"num\":0}},\"{new string('a', ChunkSize.Size8K)}\"]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray()),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xee, ChunkSize.Size8K + 1).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V4,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 1,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                        $"\"{new string('a', ChunkSize.Size8K)}\"",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray(),
                    }),
                (
                    EngineIO.V4,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes($"452-[\"hello\",{{\"_placeholder\":true,\"num\":0}},{{\"_placeholder\":true,\"num\":1}},\"{new string('a', ChunkSize.Size8K)}\"]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray()),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xee, ChunkSize.Size8K + 1).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V4,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 2,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                        "{\"_placeholder\":true,\"num\":1}",
                        $"\"{new string('a', ChunkSize.Size8K)}\"",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray(),
                        Enumerable.Repeat<byte>(0xee, ChunkSize.Size8K + 1).ToArray(),
                    }),

                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes("451-[\"hello\",{\"_placeholder\":true,\"num\":0}]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K - 1).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V3,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 1,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K - 2).ToArray(),
                    }),
                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes("451-[\"hello\",{\"_placeholder\":true,\"num\":0}]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V3,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 1,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K - 1).ToArray(),
                    }),
                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes("451-[\"hello\",{\"_placeholder\":true,\"num\":0}]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V3,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 1,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K).ToArray(),
                    }),
                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes($"451-[\"hello\",{{\"_placeholder\":true,\"num\":0}},\"{new string('a', ChunkSize.Size8K)}\"]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray()),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xee, ChunkSize.Size8K + 1).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V3,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 1,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                        $"\"{new string('a', ChunkSize.Size8K)}\"",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K).ToArray(),
                    }),
                (
                    EngineIO.V3,
                    new List<(TransportMessageType type, byte[] data)>
                    {
                        (TransportMessageType.Text, Encoding.UTF8.GetBytes($"452-[\"hello\",{{\"_placeholder\":true,\"num\":0}},{{\"_placeholder\":true,\"num\":1}},\"{new string('a', ChunkSize.Size8K)}\"]")),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K + 1).ToArray()),
                        (TransportMessageType.Binary, Enumerable.Repeat<byte>(0xee, ChunkSize.Size8K + 1).ToArray()),
                    },
                    new
                    {
                        Type = MessageType.BinaryMessage,
                        EIO = EngineIO.V3,
                        Event = "hello",
                        Protocol = TransportProtocol.WebSocket,
                        BinaryCount = 2,
                    },
                    new[]
                    {
                        "{\"_placeholder\":true,\"num\":0}",
                        "{\"_placeholder\":true,\"num\":1}",
                        $"\"{new string('a', ChunkSize.Size8K)}\"",
                    },
                    new List<byte[]>
                    {
                        Enumerable.Repeat<byte>(0xff, ChunkSize.Size8K).ToArray(),
                        Enumerable.Repeat<byte>(0xee, ChunkSize.Size8K).ToArray(),
                    }),
            };
        }
    }

    [TestMethod]
    [DynamicData(nameof(SendCases))]
    public async Task Send(EngineIO eio, Payload payload, int textTimes, int byteTimes)
    {
        var mockWs = new Mock<IClientWebSocket>();

        var transport = new WebSocketTransport(new TransportOptions
        {
            EIO = eio,
        }, mockWs.Object);
        await transport.SendAsync(payload, CancellationToken.None);

        mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Text, It.IsAny<bool>(), CancellationToken.None), Times.Exactly(textTimes));
        mockWs.Verify(e => e.SendAsync(It.IsAny<byte[]>(), TransportMessageType.Binary, It.IsAny<bool>(), CancellationToken.None), Times.Exactly(byteTimes));
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
                (EngineIO.V4, new Payload { Bytes = new List<byte[]> { new byte[] { } } }, 0, 0),
                (EngineIO.V4,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K),
                    },
                    1, 0),
                (EngineIO.V4,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K + 1),
                    },
                    2, 0),
                (EngineIO.V3,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K),
                        Bytes = new List<byte[]>
                        {
                            new byte[ChunkSize.Size8K - 1],
                        },
                    },
                    1, 1),
                (EngineIO.V4,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K),
                        Bytes = new List<byte[]>
                        {
                            new byte[ChunkSize.Size8K],
                        },
                    },
                    1, 1),
                (EngineIO.V3,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K + 1),
                        Bytes = new List<byte[]>
                        {
                            new byte[ChunkSize.Size8K - 1],
                        },
                    },
                    2, 1),
                (EngineIO.V4,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K + 1),
                        Bytes = new List<byte[]>
                        {
                            new byte[ChunkSize.Size8K],
                        },
                    },
                    2, 1),
                (EngineIO.V3,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K),
                        Bytes = new List<byte[]>
                        {
                            new byte[ChunkSize.Size8K],
                        },
                    },
                    1, 2),
                (EngineIO.V4,
                    new Payload
                    {
                        Text = new string('a', ChunkSize.Size8K),
                        Bytes = new List<byte[]>
                        {
                            new byte[ChunkSize.Size8K + 1],
                        },
                    },
                    1, 2),
            };
        }
    }

    [TestMethod]
    [DataRow(EngineIO.V3, 100)]
    [DataRow(EngineIO.V4, 100)]
    public async Task ConcurrentlySend(EngineIO eio, int times)
    {
        int extraLength = eio == EngineIO.V3 ? 2 : 1;
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
            .Setup(x => x.SendAsync(It.Is<byte[]>(b => b.Length == extraLength), TransportMessageType.Binary, true, CancellationToken.None))
            .Callback(() => (++order % 4).Should().Be(0));

        var transport = new WebSocketTransport(new TransportOptions
        {
            EIO = eio,
        }, mockWs.Object);
        var payload = new Payload
        {
            Text = new string('a', ChunkSize.Size8K + 1),
            Bytes = new List<byte[]>
            {
                new byte[ChunkSize.Size8K + 1],
            },
        };

        Parallel.For(0, times, _ => transport.SendAsync(payload, CancellationToken.None).GetAwaiter().GetResult());

        mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == ChunkSize.Size8K), TransportMessageType.Text, false, CancellationToken.None), Times.Exactly(times));
        mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == 1), TransportMessageType.Text, true, CancellationToken.None), Times.Exactly(times));
        mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == ChunkSize.Size8K), TransportMessageType.Binary, false, CancellationToken.None), Times.Exactly(times));
        mockWs.Verify(e => e.SendAsync(It.Is<byte[]>(b => b.Length == extraLength), TransportMessageType.Binary, true, CancellationToken.None), Times.Exactly(times));
    }
}