using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Session.EngineIOAdapter;

namespace SocketIOClient.Session.Http.EngineIOAdapter;

public class PollingHandler(
    IHttpAdapter httpAdapter,
    IRetriable retryPolicy,
    ILogger<PollingHandler> logger,
    IDelay delay)
    : IPollingHandler, IDisposable
{
    private OpenedMessage? _openedMessage;
    private readonly CancellationTokenSource _pollingCancellationTokenSource = new();

    public void StartPolling(OpenedMessage message, bool autoUpgrade)
    {
        if (autoUpgrade && message.Upgrades.Contains("websocket"))
        {
            return;
        }
        _openedMessage = message;
        _ = PollingAsync().ConfigureAwait(false);
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
        const int interval = 20;
        while (ms < _openedMessage!.PingInterval)
        {
            if (httpAdapter.IsReadyToSend)
            {
                return;
            }
            await delay.DelayAsync(interval, CancellationToken.None).ConfigureAwait(false);
            ms += interval;
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