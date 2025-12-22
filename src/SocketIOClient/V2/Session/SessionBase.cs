using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session;

public abstract class SessionBase : ISession
{
    protected SessionBase(
        ILogger<SessionBase> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IProtocolAdapter protocolAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory,
        IUriConverter uriConverter)
    {
        _logger = logger;
        _engineIOAdapterFactory = engineIOAdapterFactory;
        _uriConverter = uriConverter;
        _protocolAdapter = protocolAdapter;
        _serializer = serializer;
        _engineIOMessageAdapterFactory = engineIOMessageAdapterFactory;
        protocolAdapter.Subscribe(this);
    }

    private readonly ILogger<SessionBase> _logger;
    private readonly IUriConverter _uriConverter;
    private readonly IProtocolAdapter _protocolAdapter;
    private readonly ISerializer _serializer;
    private readonly IEngineIOMessageAdapterFactory _engineIOMessageAdapterFactory;
    private readonly IEngineIOAdapterFactory _engineIOAdapterFactory;
    private IEngineIOAdapter _engineIOAdapter;

    private readonly List<IMyObserver<IMessage>> _observers = [];
    private readonly Queue<IBinaryMessage> _messageQueue = [];

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

    public int PendingDeliveryCount => _messageQueue.Count;

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

    protected abstract TransportProtocol Protocol { get; }

    private void OnOptionsChanged(SessionOptions newValue)
    {
        _engineIOAdapter = _engineIOAdapterFactory.Create(newValue.EngineIO);
        _engineIOAdapter.Timeout = newValue.Timeout;
        _engineIOAdapter.Namespace = newValue.Namespace;
        _engineIOAdapter.Subscribe(this);
        var engineIOMessageAdapter = _engineIOMessageAdapterFactory.Create(newValue.EngineIO);
        _serializer.SetEngineIOMessageAdapter(engineIOMessageAdapter);

        OnEngineIOAdapterInitialized(_engineIOAdapter);
    }

    protected abstract void OnEngineIOAdapterInitialized(IEngineIOAdapter engineIOAdapter);

    public abstract Task SendAsync(object[] data, CancellationToken cancellationToken);

    public abstract Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);

    public abstract Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken);

    protected abstract Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken);

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var uri = _uriConverter.GetServerUri(
            Protocol == TransportProtocol.WebSocket,
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

    protected async Task HandleMessageAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            await OnNextBytesMessage(message.Bytes).ConfigureAwait(false);
        }
        else
        {
            await OnNextTextMessage(message.Text).ConfigureAwait(false);
        }
    }

    private async Task OnNextBytesMessage(byte[] bytes)
    {
        _logger.LogDebug("[{protocol}⬇] 0️⃣1️⃣0️⃣1️⃣ {length}", Protocol, bytes.Length);
        var message = _messageQueue.Peek();
        message.Add(bytes);
        if (message.ReadyDelivery)
        {
            _messageQueue.Dequeue();
            await OnNextAsync(message).ConfigureAwait(false);
        }
    }

    private async Task OnNextTextMessage(string text)
    {
        _logger.LogDebug("[{protocol}⬇] {text}", Protocol, text);
        var message = _serializer.Deserialize(text);
        if (message is null)
        {
            return;
        }

        switch (message.Type)
        {
            case MessageType.Binary:
            case MessageType.BinaryAck:
                _messageQueue.Enqueue((IBinaryMessage)message);
                return;
            case MessageType.Opened:
                var openedMessage = (OpenedMessage)message;
                OnOpenedMessage(openedMessage);
                break;
        }

        await _engineIOAdapter.ProcessMessageAsync(message).ConfigureAwait(false);

        await OnNextAsync(message).ConfigureAwait(false);
    }

    protected virtual void OnOpenedMessage(OpenedMessage message)
    {
    }
}