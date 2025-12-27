using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.Session.WebSocket;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Session.WebSocket.EngineIOAdapter;

public class WebSocketEngineIO3AdapterTests
{
    public WebSocketEngineIO3AdapterTests(ITestOutputHelper output)
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _webSocketAdapter = Substitute.For<IWebSocketAdapter>();
        var logger = output.CreateLogger<WebSocketEngineIO3Adapter>();
        var pollingHandler = Substitute.For<IPollingHandler>();
        _adapter = new(_stopwatch, logger, pollingHandler, _webSocketAdapter)
        {
            Options = new EngineIOAdapterOptions()
        };
    }

    private readonly IStopwatch _stopwatch;
    private readonly IWebSocketAdapter _webSocketAdapter;
    private readonly WebSocketEngineIO3Adapter _adapter;

    [Fact]
    public async Task ProcessMessageAsync_ConnectedMessage_PingInBackground()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        var range = Quantity.Within(6, 11);
        await _webSocketAdapter.Received()
            .SendAsync(Arg.Is<ProtocolMessage>(m => m.Text == "2"), Arg.Any<CancellationToken>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task ProcessMessageAsync_PongMessage_OnlySetDurationNotNotifyToSubscriber()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);
        _stopwatch.Elapsed.Returns(TimeSpan.FromSeconds(1));

        var pong = new PongMessage();
        await _adapter.ProcessMessageAsync(pong);

        await observer
            .DidNotReceive()
            .OnNextAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task StartPingAsync_ObserverThrowException_ContinuePing()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        observer.OnNextAsync(Arg.Any<IMessage>()).ThrowsAsync(new Exception());
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        var range = Quantity.Within(5, 12);
        await _webSocketAdapter.Received(range)
            .SendAsync(Arg.Is<ProtocolMessage>(m => m.Text == "2"), Arg.Any<CancellationToken>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task StartPingAsync_WhenCalled_FirstDelayThenPing()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 100,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(50);

        await _webSocketAdapter.DidNotReceive()
            .SendAsync(Arg.Any<ProtocolMessage>(), Arg.Any<CancellationToken>());
        await observer
            .DidNotReceive()
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task StartPingAsync_DisposeIsCalled_NeverPing()
    {
        _adapter.Dispose();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(200);

        await _webSocketAdapter.DidNotReceive()
            .SendAsync(Arg.Any<ProtocolMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ConnectedMessageButNoOpenedMessage_ThrowException()
    {
        var connectedMessage = new ConnectedMessage();
        await _adapter.Invoking(x => x.ProcessMessageAsync(connectedMessage))
            .Should()
            .ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task ProcessMessageAsync_OpenedMessageThenConnectedMessage_SidIsNotNull()
    {
        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            Sid = "123",
        });
        var connectedMessage = new ConnectedMessage();
        await _adapter.ProcessMessageAsync(connectedMessage);
        connectedMessage.Sid.Should().Be("123");
    }

    [Theory]
    [InlineData(null, "40")]
    [InlineData("", "40")]
    public async Task ProcessMessageAsync_ReceivedOpenedMessage_NotSendConnectedMessage(string nsp, string expected)
    {
        _adapter.Options.Namespace = nsp;
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _webSocketAdapter.DidNotReceive()
            .SendAsync(Arg.Any<ProtocolMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ReceivedOpenedMessage_SendConnectedMessage()
    {
        _adapter.Options.Namespace = "/nsp";
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _webSocketAdapter.Received()
            .SendAsync(Arg.Is<ProtocolMessage>(m => m.Text == "40/nsp,"),
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/nsp", null, true)]
    [InlineData("/nsp", "", true)]
    [InlineData("/nsp", "/", true)]
    [InlineData("/nsp", "/abc", true)]
    [InlineData("/nsp", "/nsp", false)]
    [InlineData("/nsp", "/NSP", false)]
    [InlineData(null, null, false)]
    [InlineData("", null, false)]
    [InlineData(null, "", false)]
    [InlineData("", "/", false)]
    [InlineData("/", "", false)]
    [InlineData("/", null, false)]
    public async Task ProcessMessageAsync_NamespaceAndWhetherSwallow_AlwaysPass(string adapterNsp, string connNsp, bool shouldSwallow)
    {
        _adapter.Options.Namespace = adapterNsp;
        var message = new ConnectedMessage
        {
            Namespace = connNsp,
        };

        await _adapter.ProcessMessageAsync(new OpenedMessage());
        var result = await _adapter.ProcessMessageAsync(message);

        result.Should().Be(shouldSwallow);
    }

    [Fact]
    public async Task ProcessMessageAsync_OnlyReceivedSwallowedConnectedMessage_NeverStartPing()
    {
        _adapter.Options.Namespace = "/nsp";
        var message = new ConnectedMessage();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(message);
        await Task.Delay(100);

        await _webSocketAdapter.DidNotReceive()
            .SendAsync(Arg.Is<ProtocolMessage>(m => m.Text == "2"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ReceivedConnectedMessageWithNamespace_StartPing()
    {
        _adapter.Options.Namespace = "/nsp";
        var message = new ConnectedMessage
        {
            Namespace = "/nsp",
        };

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 80 });
        await _adapter.ProcessMessageAsync(message);
        await Task.Delay(100);

        await _webSocketAdapter.Received()
            .SendAsync(Arg.Is<ProtocolMessage>(m => m.Text == "2"),
                Arg.Any<CancellationToken>());
    }
}