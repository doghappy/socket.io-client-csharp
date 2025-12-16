using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session;

public abstract class SessionBase : ISession
{
    protected SessionBase(ILogger<SessionBase> logger, IUriConverter uriConverter, IProtocolAdapter protocolAdapter)
    {
        _logger = logger;
        _uriConverter = uriConverter;
        _protocolAdapter = protocolAdapter;
        protocolAdapter.Subscribe(this);
    }

    private readonly ILogger<SessionBase> _logger;
    private readonly IUriConverter _uriConverter;
    private readonly IProtocolAdapter _protocolAdapter;

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
        _logger.LogDebug("Deliver message to SocketIO, Type: {Type}", message.Type);
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

    protected abstract Core.Protocol Protocol { get; }

    protected abstract void OnOptionsChanged(SessionOptions newValue);

    public abstract Task SendAsync(object[] data, CancellationToken cancellationToken);

    public abstract Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);

    public abstract Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken);

    protected abstract Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken);

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var uri = _uriConverter.GetServerUri(
            Protocol == Core.Protocol.WebSocket,
            Options.ServerUri,
            Options.Path,
            Options.Query,
            (int)Options.EngineIO);
        if (Options.ExtraHeaders is not null)
        {
            foreach (var header in Options.ExtraHeaders)
            {
                _protocolAdapter.SetDefaultHeader(header.Key, header.Value);
            }
        }

        try
        {
            await ConnectCoreAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new ConnectionFailedException(e);
        }
    }

    public abstract Task DisconnectAsync(CancellationToken cancellationToken);
}