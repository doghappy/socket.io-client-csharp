using FluentAssertions;
using NSubstitute;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.WebSocket;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.SystemTextJson;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.WebSocket.EngineIOAdapter;

namespace SocketIOClient.UnitTests.Session.WebSocket.EngineIOAdapter;

public class WebSocketEngineIO4AdapterTests
{
    public WebSocketEngineIO4AdapterTests()
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _webSocketAdapter = Substitute.For<IWebSocketAdapter>();
        _serializer = Substitute.For<ISerializer>();
        _adapter = new(_stopwatch, _serializer, _webSocketAdapter)
        {
            Options = new EngineIOAdapterOptions()
        };
    }

    private readonly IStopwatch _stopwatch;
    private readonly IWebSocketAdapter _webSocketAdapter;
    private readonly WebSocketEngineIO4Adapter _adapter;
    private readonly ISerializer _serializer;

    [Fact]
    public async Task ProcessMessageAsync_PingMessage_NotifyObserverWithDuration()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);
        _stopwatch.Elapsed.Returns(TimeSpan.FromSeconds(1));

        await _adapter.ProcessMessageAsync(new PingMessage());

        await observer
            .Received(1)
            .OnNextAsync(Arg.Is<IMessage>(m => ((PongMessage)m).Duration == TimeSpan.FromSeconds(1)));
    }

    [Theory]
    [InlineData(null, "40")]
    [InlineData("", "40")]
    [InlineData("/nsp", "40/nsp,")]
    public async Task ProcessMessageAsync_ReceivedOpenedMessage_SendConnectedMessage(string nsp, string expected)
    {
        _adapter.Options.Namespace = nsp;
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _webSocketAdapter.Received()
            .SendAsync(Arg.Is<ProtocolMessage>(r => r.Text == expected), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "40{auth}")]
    [InlineData("/nsp", "40/nsp,{auth}")]
    public async Task ProcessMessageAsync_AuthIsProvided_ConnectedMessageContainsAuth(string nsp, string expected)
    {
        _adapter.Options.Namespace = nsp;
        _adapter.Options.Auth = new { user = "admin", password = "123456" };
        _serializer.Serialize(Arg.Any<object>()).Returns("{auth}");
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _webSocketAdapter.Received()
            .SendAsync(Arg.Is<ProtocolMessage>(r => r.Text == expected), Arg.Any<CancellationToken>());
    }

    private static IEnumerable<IMessage> ProcessMessageAsyncMessageTypeTupleCases()
    {
        yield return new OpenedMessage();
        yield return new PingMessage();
        yield return new PongMessage();
        yield return new ConnectedMessage();
        yield return new DisconnectedMessage();
        yield return new SystemJsonEventMessage();
        yield return new SystemJsonAckMessage();
        yield return new ErrorMessage();
        yield return new SystemJsonBinaryEventMessage();
        yield return new SystemJsonBinaryAckMessage();
    }

    public static IEnumerable<object[]> ProcessMessageAsyncMessageTypeCases =>
        ProcessMessageAsyncMessageTypeTupleCases().Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(ProcessMessageAsyncMessageTypeCases))]
    public async Task ProcessMessageAsync_RegardlessOfMessageType_AlwaysReturnFalse(IMessage message)
    {
        var result = await _adapter.ProcessMessageAsync(message);
        result.Should().BeFalse();
    }

    [Fact]
    public void WriteProtocolFrame_WhenCalled_ReturnSameValue()
    {
        var result = _adapter.WriteProtocolFrame([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        result.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
    }

    [Fact]
    public void ReadProtocolFrame_WhenCalled_ReturnSameValue()
    {
        var result = _adapter.ReadProtocolFrame([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        result.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
    }
}