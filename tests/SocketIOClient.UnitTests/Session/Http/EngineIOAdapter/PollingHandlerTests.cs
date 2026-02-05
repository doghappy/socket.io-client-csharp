using NSubstitute;
using SocketIOClient.Common.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Session.Http.EngineIOAdapter;
using SocketIOClient.Test.Core;
using SocketIOClient.UnitTests.Fakes;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Session.Http.EngineIOAdapter;

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
        _fakeDelay = new FakeDelay(output);
        _pollingHandler = new PollingHandler(_httpAdapter, _retryPolicy, logger, _fakeDelay);
    }

    private readonly PollingHandler _pollingHandler;
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;
    private readonly FakeDelay _fakeDelay;

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
            PingInterval = 100,
        }, false);

        await Task.Delay(500);

        await _retryPolicy
            .Received(1)
            .RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_IsReadyFirstFalseThenTrue_PollingIsWorking()
    {
        _httpAdapter.IsReadyToSend.Returns(false);
        _ = Task.Run(async () =>
        {
            await _fakeDelay.DelayAsync(21, CancellationToken.None);
            _httpAdapter.IsReadyToSend.Returns(true);
            await _fakeDelay.DelayAsync(22, CancellationToken.None);
        });
        _pollingHandler.StartPolling(new OpenedMessage { PingInterval = 2000 }, false);


        await _fakeDelay.AdvanceAsync(20);
        await _fakeDelay.AdvanceAsync(21);
        await _fakeDelay.AdvanceAsync(22);
        await _fakeDelay.AdvanceAsync(20);

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