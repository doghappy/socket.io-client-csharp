using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Session;

public abstract class SessionBase(ILogger<SessionBase> logger) : ISession
{
    private readonly List<IMyObserver<IMessage>> _observers = [];
    protected Queue<IBinaryMessage> MessageQueue { get; } = [];

    public void Subscribe(IMyObserver<IMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }

    public abstract Task OnNextAsync(ProtocolMessage message);

    public async Task OnNextAsync(IMessage message)
    {
        logger.LogDebug("Deliver message to SocketIO, Type: {Type}", message.Type);
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    public int PendingDeliveryCount => MessageQueue.Count;

    private SessionOptions _options;
    public SessionOptions Options
    {
        get => _options;
        set
        {
            _options = value;
            OnOptionsChanged(value);
        }
    }

    protected abstract void OnOptionsChanged(SessionOptions newValue);

    public abstract Task SendAsync(object[] data, CancellationToken cancellationToken);

    public abstract Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);

    public abstract Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken);

    public abstract Task ConnectAsync(CancellationToken cancellationToken);

    public abstract Task DisconnectAsync(CancellationToken cancellationToken);
}