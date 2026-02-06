using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.Protocol.WebSocket;
using SysWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;
using WebSocketMessageType = SocketIOClient.Protocol.WebSocket.WebSocketMessageType;

namespace SocketIOClient.UnitTests.Protocol.WebSocket;

public class SystemClientWebSocketAdapterTests
{
    public SystemClientWebSocketAdapterTests()
    {
        _ws = Substitute.For<IWebSocketClient>();
        _adapter = new SystemClientWebSocketAdapter(_ws)
        {
            SendChunkSize = 1024,
            ReceiveChunkSize = 1024,
        };
    }

    private readonly SystemClientWebSocketAdapter _adapter;
    private readonly IWebSocketClient _ws;

    [Theory(Timeout = 5000)]
    [InlineData(0, 1)]
    [InlineData(1024, 1)]
    [InlineData(1025, 2)]
    [InlineData(2048, 2)]
    public async Task SendAsync_LongTextMessage_PartitionData(int length, int expectedSegments)
    {
        var text = new string('a', length);
        var data = Encoding.UTF8.GetBytes(text);
        await _adapter.SendAsync(data, WebSocketMessageType.Text, CancellationToken.None);

        await _ws.Received(expectedSegments - 1).SendAsync(
            Arg.Any<ArraySegment<byte>>(),
            SysWebSocketMessageType.Text,
            false,
            CancellationToken.None);

        await _ws.Received(1).SendAsync(
            Arg.Any<ArraySegment<byte>>(),
            SysWebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    [Fact(Timeout = 5000)]
    public async Task SendAsync_ByteMessage_PartitionData()
    {
        var data = new byte[1025];
        await _adapter.SendAsync(data, WebSocketMessageType.Binary, CancellationToken.None);

        await _ws.Received(1).SendAsync(
            Arg.Any<ArraySegment<byte>>(),
            SysWebSocketMessageType.Binary,
            false,
            CancellationToken.None);

        await _ws.Received(1).SendAsync(
            Arg.Any<ArraySegment<byte>>(),
            SysWebSocketMessageType.Binary,
            true,
            CancellationToken.None);
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_WhenCalled_ThoughPassToWebSocketClient()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        await _adapter.ConnectAsync(new Uri("ws://localhost:12345"), token);

        await _ws.Received().ConnectAsync(new Uri("ws://localhost:12345"), token);
    }

    [Theory(Timeout = 5000)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1023)]
    [InlineData(1024)]
    public async Task ReceiveAsync_ActualDataLengthLessOrEqualThanReceiveChunkSize_ReturnActualLength(int length)
    {
        _ws.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
            .Returns(new WebSocketReceiveResult(length, SysWebSocketMessageType.Text, true));

        var message = await _adapter.ReceiveAsync(CancellationToken.None);

        message.Type.Should().Be(WebSocketMessageType.Text);
        message.Bytes.Should().HaveCount(length);
    }

    [Theory(Timeout = 5000)]
    [InlineData(1025)]
    [InlineData(2048)]
    public async Task ReceiveAsync_ActualDataLengthGreaterThanReceiveChunkSize_ReturnActualLength(int length)
    {
        _ws.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebSocketReceiveResult(1024, SysWebSocketMessageType.Binary, false),
                new WebSocketReceiveResult(length - 1024, SysWebSocketMessageType.Binary, true));

        var message = await _adapter.ReceiveAsync(CancellationToken.None);

        message.Type.Should().Be(WebSocketMessageType.Binary);
        message.Bytes.Should().HaveCount(length);
    }

    [Fact]
    public void SetDefaultHeader_WhenCalled_AlwaysWsClientSetDefaultHeader()
    {
        _adapter.SetDefaultHeader("name", "value");

        _ws.Received().SetDefaultHeader("name", "value");
    }
}