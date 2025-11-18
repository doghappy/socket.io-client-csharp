using FluentAssertions;
using SocketIOClient.V2.Protocol.WebSocket;

namespace SocketIOClient.UnitTests.V2.Protocol.WebSocket;

public class WebSocketMessageTests
{
    private readonly WebSocketMessage _webSocketMessage = new();

    [Fact]
    public void DefaultValues()
    {
        _webSocketMessage.Should().BeEquivalentTo(new WebSocketMessage
        {
            Bytes = null,
            Count = 0,
            EndOfMessage = false,
            Type = WebSocketMessageType.Text,
        });
    }
}