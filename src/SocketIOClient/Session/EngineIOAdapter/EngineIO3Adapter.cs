using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Observers;

namespace SocketIOClient.Session.EngineIOAdapter;

public abstract class EngineIO3Adapter : IEngineIOAdapter, IDisposable
{
    protected EngineIO3Adapter(IStopwatch stopwatch, ILogger<EngineIO3Adapter> logger)
    {
        _stopwatch = stopwatch;
        _logger = logger;
    }

    private readonly IStopwatch _stopwatch;
    private readonly ILogger<EngineIO3Adapter> _logger;
    private readonly CancellationTokenSource _pingCancellationTokenSource = new();
    private readonly List<IMyObserver<IMessage>> _observers = [];

    private static readonly HashSet<string?> DefaultNamespaces =
    [
        null,
        string.Empty,
        "/"
    ];

    private OpenedMessage? OpenedMessage { get; set; }
    public EngineIOAdapterOptions Options { get; set; } = null!;

    protected abstract Task SendConnectAsync();
    protected abstract Task SendPingAsync();

    protected virtual Task OnPingTaskStarted()
    {
        return Task.CompletedTask;
    }

    protected virtual void OnOpenedMessageReceived(OpenedMessage message)
    {
    }

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
        OnOpenedMessageReceived(OpenedMessage);
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
                            && !Options.Namespace!.Equals(connectedMessage.Namespace, StringComparison.InvariantCultureIgnoreCase);
        if (!shouldSwallow)
        {
            connectedMessage.Sid = OpenedMessage!.Sid;
            Task.Run(StartPingAsync);
        }

        return shouldSwallow;
    }

    private async Task StartPingAsync()
    {
        await OnPingTaskStarted().ConfigureAwait(false);
        var token = _pingCancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            _logger.LogDebug("===========");
            await Task.Delay(OpenedMessage!.PingInterval, token).ConfigureAwait(false);
            _logger.LogDebug("Sending Ping...");
            await SendPingAsync().ConfigureAwait(false);
            _logger.LogDebug("Sent Ping");
            _stopwatch.Restart();
            _ = NotifyObserversAsync(new PingMessage());
        }
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

    public void Unsubscribe(IMyObserver<IMessage> observer)
    {
        _observers.Remove(observer);
    }

    public void Dispose()
    {
        _pingCancellationTokenSource.Cancel();
        _pingCancellationTokenSource.Dispose();
    }
}