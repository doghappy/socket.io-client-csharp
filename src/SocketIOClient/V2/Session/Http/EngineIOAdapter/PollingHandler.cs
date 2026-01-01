using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.Http.EngineIOAdapter;

public class PollingHandler(IHttpAdapter httpAdapter, IRetriable retryPolicy, ILogger<PollingHandler> logger)
    : IPollingHandler, IDisposable
{
    private OpenedMessage _openedMessage;
    private readonly CancellationTokenSource _pollingCancellationTokenSource = new();

    public void StartPolling(OpenedMessage message)
    {
        _openedMessage = message;
        _ = Task.Run(PollingAsync);
    }

    private async Task PollingAsync()
    {
        logger.LogDebug("[PollingAsync] Waiting for HttpAdapter ready...");
        await WaitHttpAdapterReady().ConfigureAwait(false);
        logger.LogDebug("[PollingAsync] HttpAdapter is ready");
        var token = _pollingCancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            var request = new HttpRequest();
            logger.LogDebug("Send Polling request...");
            await retryPolicy.RetryAsync(2, async () =>
            {
                await httpAdapter.SendAsync(request, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
            logger.LogDebug("Sent Polling request");
        }
    }

    public async Task WaitHttpAdapterReady()
    {
        var ms = 0;
        const int delay = 20;
        while (ms < _openedMessage.PingInterval)
        {
            if (httpAdapter.IsReadyToSend)
            {
                return;
            }
            await Task.Delay(delay).ConfigureAwait(false);
            ms += delay;
        }
        var ex = new TimeoutException();
        logger.LogError(ex, "Wait HttpAdapter ready timeout");
        throw ex;
    }

    public void Dispose()
    {
        _pollingCancellationTokenSource.Cancel();
        _pollingCancellationTokenSource.Dispose();
    }
}