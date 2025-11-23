using System.Text;
using NSubstitute;
using SocketIOClient.V2.Protocol.WebSocket;
using SysWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace SocketIOClient.UnitTests.V2.Protocol.WebSocket;

public class SystemClientWebSocketAdapterTests
{
    public SystemClientWebSocketAdapterTests()
    {
        _ws = Substitute.For<IWebSocketClient>();
        _adapter = new SystemClientWebSocketAdapter(_ws)
        {
            SendChunkSize = 1024,
        };
    }

    private readonly SystemClientWebSocketAdapter _adapter;
    private readonly IWebSocketClient _ws;

    [Theory]
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
}