using Microsoft.Extensions.Logging;
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
        _logger = output.CreateLogger<PollingHandler>();
        _pollingHandler = new PollingHandler(_httpAdapter, _retryPolicy, _logger);
    }

    private readonly PollingHandler _pollingHandler;
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;
    private readonly ILogger<PollingHandler> _logger;

    [Fact]
    public async Task OnOpenedMessageReceived_WhenNeverCalled_NotStartPolling()
    {
        await Task.Delay(100);
        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task OnOpenedMessageReceived_WhenCalled_StartPolling()
    {
        _pollingHandler.OnOpenedMessageReceived(new OpenedMessage
        {
            PingInterval = 50
        });
        await Task.Delay(100);
        await _retryPolicy.Received().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_FirstDisposeThenOnOpenedMessageReceived_NeverStartPolling()
    {
        _pollingHandler.Dispose();

        _pollingHandler.OnOpenedMessageReceived(new OpenedMessage
        {
            PingInterval = 50
        });

        await Task.Delay(100);
        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_HttpRequestExceptionOccurred_DoNotContinue()
    {
        _retryPolicy.RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>())
            .Returns(_ => Task.FromException(new HttpRequestException()));

        _pollingHandler.OnOpenedMessageReceived(new OpenedMessage
        {
            PingInterval = 10,
        });

        await Task.Delay(100);

        await _retryPolicy
            .Received(1)
            .RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_IsReadyAfter30ms_PollingIsWorking()
    {
        _httpAdapter.IsReadyToSend.Returns(false);

        _pollingHandler.OnOpenedMessageReceived(new OpenedMessage { PingInterval = 100 });
        _ = Task.Run(async () =>
        {
            await Task.Delay(30);
            _httpAdapter.IsReadyToSend.Returns(true);
        });

        await Task.Delay(100);

        await _retryPolicy.Received().RetryAsync(2, Arg.Any<Func<Task>>());
    }
}