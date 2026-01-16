using System.Text;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using SocketIOClient.Core;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.WebSocket;
using SocketIOClient.Test.Core;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Protocol.WebSocket;

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

    [Fact]
    public async Task ConnectAsync_WhenCalled_ThoughPassToClientAdapter()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        await _wsAdapter.ConnectAsync(new Uri("ws://127.0.0.1:1234"), token);

        await _clientAdapter.Received().ConnectAsync(new Uri("ws://127.0.0.1:1234"), token);
    }

    [Fact]
    public async Task ConnectAsync_NotConnectedEvenReceivedMessage_ObserverCannotGetMessage()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _wsAdapter.Subscribe(observer);

        _clientAdapter.ReceiveAsync(CancellationToken.None)
            .Returns(new WebSocketMessage());

        await observer.DidNotReceive().OnNextAsync(Arg.Any<ProtocolMessage>());
    }

    [Fact]
    public async Task ConnectAsync_ReceivedTextMessage_NotifyToObserver()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _wsAdapter.Subscribe(observer);
        _clientAdapter.ReceiveAsync(Arg.Is<CancellationToken>(c => c != CancellationToken.None))
            .Returns(new WebSocketMessage
            {
                Type = WebSocketMessageType.Text,
                Bytes = "Hello World!"u8.ToArray()
            });

        await _wsAdapter.ConnectAsync(new Uri("ws://127.0.0.1:1234"), CancellationToken.None);

        await Task.Delay(10);

        await observer.Received()
            .OnNextAsync(Arg.Is<ProtocolMessage>(m =>
                m.Type == ProtocolMessageType.Text
                && m.Text == "Hello World!"));
    }

    [Fact]
    public void SetDefaultHeader_WhenCalled_AlwaysCallClientAdapterSetDefaultHeader()
    {
        _wsAdapter.SetDefaultHeader("name", "value");

        _clientAdapter.Received().SetDefaultHeader("name", "value");
    }

    [Fact]
    public async Task Dispose_WhenCalled_StopsReceivingFurtherMessages()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _wsAdapter.Subscribe(observer);
        _clientAdapter.ReceiveAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(10);
                return new WebSocketMessage
                {
                    Type = WebSocketMessageType.Text,
                    Bytes = "Hello World!"u8.ToArray()
                };
            });

        await _wsAdapter.ConnectAsync(new Uri("ws://127.0.0.1:1234"), CancellationToken.None);
        _wsAdapter.Dispose();

        await Task.Delay(50);

        await observer.Received(Quantity.Within(0, 1)).OnNextAsync(Arg.Any<ProtocolMessage>());
    }
}