using NSubstitute;
using SocketIOClient.Core.Messages;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.Http.EngineIOAdapter;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Session.Http.EngineIOAdapter;

public class PollingHandlerTests
{
    public PollingHandlerTests(ITestOutputHelper output)
    {
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _httpAdapter.IsReadyToSend.Returns(true);
        _retryPolicy = Substitute.For<IRetriable>();
        _retryPolicy.RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>()).Returns(async _ =>
        {
            await Task.Delay(20);
        });
        var logger = output.CreateLogger<PollingHandler>();
        _pollingHandler = new PollingHandler(_httpAdapter, _retryPolicy, logger);
    }

    private readonly PollingHandler _pollingHandler;
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;

    [Fact]
    public async Task StartPolling_WhenNeverCalled_DoNotSendHttpRequest()
    {
        await Task.Delay(100);
        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task StartPolling_WhenCalled_SendHttpRequest()
    {
        _pollingHandler.StartPolling(new OpenedMessage
        {
            PingInterval = 50
        }, false);
        await Task.Delay(100);
        await _retryPolicy.Received().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_FirstDisposeThenStartPolling_NeverStartPolling()
    {
        _pollingHandler.Dispose();

        _pollingHandler.StartPolling(new OpenedMessage
        {
            PingInterval = 50
        }, false);

        await Task.Delay(100);
        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_HttpRequestExceptionOccurred_DoNotContinue()
    {
        _retryPolicy.RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>())
            .Returns(_ => Task.FromException(new HttpRequestException()));

        _pollingHandler.StartPolling(new OpenedMessage
        {
            PingInterval = 10,
        }, false);

        await Task.Delay(100);

        await _retryPolicy
            .Received(1)
            .RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_IsReadyAfter30ms_PollingIsWorking()
    {
        _httpAdapter.IsReadyToSend.Returns(false);

        _pollingHandler.StartPolling(new OpenedMessage { PingInterval = 100 }, false);
        _ = Task.Run(async () =>
        {
            await Task.Delay(30);
            _httpAdapter.IsReadyToSend.Returns(true);
        });

        await Task.Delay(100);

        await _retryPolicy.Received().RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StartPolling_ServerNotSupportsWebSocket_StartPolling(bool autoUpgrade)
    {
        _pollingHandler.StartPolling(new OpenedMessage
        {
            PingInterval = 100,
            Upgrades = []
        }, autoUpgrade);

        await Task.Delay(100);
        await _retryPolicy.Received().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task StartPolling_ServerSupportsWebSocketButClientNot_StartPollingReturnTrue()
    {
        _pollingHandler.StartPolling(new OpenedMessage
        {
            PingInterval = 100,
            Upgrades = ["websocket"]
        }, false);

        await Task.Delay(100);
        await _retryPolicy.Received().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task StartPolling_BothServerAndClientSupportWebSocket_NeverStartPollingReturnFalse()
    {
        _pollingHandler.StartPolling(new OpenedMessage
        {
            PingInterval = 100,
            Upgrades = ["websocket"]
        }, true);

        await Task.Delay(100);
        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }
}