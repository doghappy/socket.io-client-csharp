using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Common.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Observers;
using SocketIOClient.Serializer;

namespace SocketIOClient.Session.EngineIOAdapter;

public abstract class EngineIO4Adapter : IEngineIOAdapter, IDisposable
{
    protected EngineIO4Adapter(IStopwatch stopwatch, ISerializer serializer)
    {
        _stopwatch = stopwatch;
        _serializer = serializer;
    }

    private readonly IStopwatch _stopwatch;
    private readonly ISerializer _serializer;
    private readonly CancellationTokenSource _pingCancellationTokenSource = new();
    private readonly List<IMyObserver<IMessage>> _observers = [];

    private OpenedMessage? OpenedMessage { get; set; }
    public EngineIOAdapterOptions Options { get; set; } = null!;

    protected abstract Task SendConnectAsync(string message);
    protected abstract Task SendPongAsync();

    protected virtual void OnOpenedMessageReceived(OpenedMessage message)
    {
    }

    public async Task<bool> ProcessMessageAsync(IMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Ping:
                await HandlePingMessageAsync().ConfigureAwait(false);
                break;
            case MessageType.Opened:
                await HandleOpenedMessageAsync(message).ConfigureAwait(false);
                break;
        }

        return false;
    }

    private async Task HandleOpenedMessageAsync(IMessage message)
    {
        OpenedMessage = (OpenedMessage)message;
        OnOpenedMessageReceived(OpenedMessage);

        var builder = new StringBuilder("40");
        if (!string.IsNullOrEmpty(Options.Namespace))
        {
            builder.Append(Options.Namespace).Append(',');
        }
        if (Options.Auth is not null)
        {
            builder.Append(_serializer.Serialize(Options.Auth));
        }
        await SendConnectAsync(builder.ToString()).ConfigureAwait(false);
    }

    private async Task HandlePingMessageAsync()
    {
        _stopwatch.Restart();
        await SendPongAsync().ConfigureAwait(false);
        _stopwatch.Stop();
        var pong = new PongMessage
        {
            Duration = _stopwatch.Elapsed,
        };
        await NotifyObserversAsync(pong).ConfigureAwait(false);
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