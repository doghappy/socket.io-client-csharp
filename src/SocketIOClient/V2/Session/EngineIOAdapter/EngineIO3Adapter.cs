using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public abstract class EngineIO3Adapter : IEngineIOAdapter, IDisposable
{
    protected EngineIO3Adapter(IStopwatch stopwatch, ILogger<EngineIO3Adapter> logger, IPollingHandler pollingHandler)
    {
        _stopwatch = stopwatch;
        _logger = logger;
        PollingHandler = pollingHandler;
    }

    private readonly IStopwatch _stopwatch;
    private readonly ILogger<EngineIO3Adapter> _logger;
    protected readonly IPollingHandler PollingHandler;
    private readonly CancellationTokenSource _pingCancellationTokenSource = new();
    private readonly List<IMyObserver<IMessage>> _observers = [];

    private static readonly HashSet<string> DefaultNamespaces =
    [
        null,
        string.Empty,
        "/"
    ];

    private OpenedMessage OpenedMessage { get; set; }
    public EngineIOAdapterOptions Options { get; set; }

    protected abstract Task SendConnectAsync();
    protected abstract Task SendPingAsync();

    public async Task<bool> ProcessMessageAsync(IMessage message)
    {
        bool shouldSwallow = false;
        switch (message.Type)
        {
            case MessageType.Opened:
                await HandleOpenedMessageAsync(message).ConfigureAwait(false);
                break;
            case MessageType.Connected:
                shouldSwallow = HandleConnectedMessageAsync(message);
                break;
            case MessageType.Pong:
                HandlePongMessage(message);
                break;
        }

        return shouldSwallow;
    }

    private async Task HandleOpenedMessageAsync(IMessage message)
    {
        OpenedMessage = (OpenedMessage)message;
        PollingHandler.OnOpenedMessageReceived(OpenedMessage);
        if (string.IsNullOrEmpty(Options.Namespace))
        {
            return;
        }
        await SendConnectAsync().ConfigureAwait(false);
    }

    private bool HandleConnectedMessageAsync(IMessage message)
    {
        var connectedMessage = (ConnectedMessage)message;
        var shouldSwallow = !DefaultNamespaces.Contains(Options.Namespace)
                            && !Options.Namespace.Equals(connectedMessage.Namespace, StringComparison.InvariantCultureIgnoreCase);
        if (!shouldSwallow)
        {
            connectedMessage.Sid = OpenedMessage.Sid;
            _ = Task.Run(StartPingAsync);
        }

        return shouldSwallow;
    }

    private async Task StartPingAsync()
    {
        await OnPingTaskStarted().ConfigureAwait(false);
        var token = _pingCancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(OpenedMessage.PingInterval, token);
            _logger.LogDebug("Sending Ping...");
            await SendPingAsync().ConfigureAwait(false);
            _logger.LogDebug("Sent Ping");
            _stopwatch.Restart();
            _ = NotifyObserversAsync(new PingMessage());
        }
    }

    protected virtual Task OnPingTaskStarted()
    {
        return Task.CompletedTask;
    }

    private void HandlePongMessage(IMessage message)
    {
        _stopwatch.Stop();
        var pongMessage = (PongMessage)message;
        pongMessage.Duration = _stopwatch.Elapsed;
    }

    private async Task NotifyObserversAsync(IMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    public void Subscribe(IMyObserver<IMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }

    public void Dispose()
    {
        _pingCancellationTokenSource.Cancel();
        _pingCancellationTokenSource.Dispose();
    }
}