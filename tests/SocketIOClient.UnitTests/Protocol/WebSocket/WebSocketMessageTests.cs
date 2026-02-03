using FluentAssertions;
using SocketIOClient.Protocol.WebSocket;

namespace SocketIOClient.UnitTests.Protocol.WebSocket;

public class WebSocketMessageTests
{
    private readonly WebSocketMessage _webSocketMessage = new();

    [Fact]
    public void DefaultValues()
    {
        _webSocketMessage.Should().BeEquivalentTo(new WebSocketMessage
        {
            Bytes = null!,
            Type = WebSocketMessageType.Text,
        });
    }
}