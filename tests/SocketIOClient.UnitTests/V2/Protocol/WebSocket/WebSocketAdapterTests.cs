using System.Text;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.Core;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Protocol.WebSocket;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Protocol.WebSocket;

public class WebSocketAdapterTests
{
    public WebSocketAdapterTests(ITestOutputHelper output)
    {
        var logger = output.CreateLogger<WebSocketAdapter>();
        _clientAdapter = Substitute.For<IWebSocketClientAdapter>();
        _wsAdapter = new WebSocketAdapter(logger, _clientAdapter);
    }

    private readonly WebSocketAdapter _wsAdapter;
    private readonly IWebSocketClientAdapter _clientAdapter;

    [Fact]
    public async Task SendAsync_TextMessageButTextIsNull_ThrowsException()
    {
        await _wsAdapter.Invoking(async x =>
            {
                var message = new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                };
                await x.SendAsync(message, CancellationToken.None);
            })
            .Should()
            .ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_ByteMessageButBytesIsNull_ThrowsException()
    {
        await _wsAdapter.Invoking(async x =>
            {
                var message = new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                };
                await x.SendAsync(message, CancellationToken.None);
            })
            .Should()
            .ThrowExactlyAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("🐮🍺")]
    public async Task SendAsync_TextMessage_ThoughPassToClientAdapter(string text)
    {
        var message = new ProtocolMessage
        {
            Text = text
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        await _wsAdapter.SendAsync(message, token);

        var bytes = Encoding.UTF8.GetBytes(text);
        await _clientAdapter.Received(1).SendAsync(
            Arg.Is<byte[]>(b => b.SequenceEqual(bytes)),
            WebSocketMessageType.Text,
            token);
    }

    [Fact]
    public async Task SendAsync_ByteMessageAndEmpty_ThoughPassToClientAdapter()
    {
        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = [],
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        await _wsAdapter.SendAsync(message, token);

        await _clientAdapter.Received(1).SendAsync(
            Arg.Is<byte[]>(b => b.Length == 0),
            WebSocketMessageType.Binary,
            token);
    }

    [Fact]
    public async Task SendAsync_ByteMessage_ThoughPassToClientAdapter()
    {
        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = [1, 255],
        };
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        await _wsAdapter.SendAsync(message, token);

        await _clientAdapter.Received(1).SendAsync(
            Arg.Is<byte[]>(b => b.Length == 2 && b[0] == 1 && b[1] == 255),
            WebSocketMessageType.Binary,
            token);
    }
}