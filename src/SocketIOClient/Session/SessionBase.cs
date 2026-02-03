using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Observers;
using SocketIOClient.Protocol;
using SocketIOClient.Serializer;
using SocketIOClient.Session.EngineIOAdapter;

namespace SocketIOClient.Session;

public abstract class SessionBase<T> : ISession where T : class, IEngineIOAdapter
{
    protected SessionBase(
        ILogger<SessionBase<T>> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IProtocolAdapter protocolAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory)
    {
        _logger = logger;
        _engineIOAdapterFactory = engineIOAdapterFactory;
        _protocolAdapter = protocolAdapter;
        _serializer = serializer;
        _engineIOMessageAdapterFactory = engineIOMessageAdapterFactory;
        protocolAdapter.Subscribe(this);
    }

    private const string DefaultPath = "/socket.io/";

    private readonly ILogger<SessionBase<T>> _logger;
    private readonly IProtocolAdapter _protocolAdapter;
    private readonly ISerializer _serializer;
    private readonly IEngineIOMessageAdapterFactory _engineIOMessageAdapterFactory;
    private readonly IEngineIOAdapterFactory _engineIOAdapterFactory;
    protected T EngineIOAdapter { get; private set; } = null!;

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

    public void Unsubscribe(IMyObserver<IMessage> observer)
    {
        _observers.Remove(observer);
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

    private SessionOptions _options = null!;

    public SessionOptions Options
    {
        get => _options;
        set
        {
            _options = value;
            OnOptionsChanged(value);
        }
    }

    private Action _onDisconnected = null!;
    public Action OnDisconnected
    {
        get => _onDisconnected;
        set
        {
            _onDisconnected = value;
            _protocolAdapter.OnDisconnected = value;
        }
    }

    protected abstract TransportProtocol Protocol { get; }

    private void OnOptionsChanged(SessionOptions newValue)
    {
        var compatibility = GetEngineIOCompatibility(newValue);
        EngineIOAdapter = _engineIOAdapterFactory.Create<T>(compatibility);
        EngineIOAdapter.Options = new EngineIOAdapterOptions
        {
            Timeout = newValue.Timeout,
            Namespace = newValue.Namespace,
            Auth = newValue.Auth,
            AutoUpgrade = newValue.AutoUpgrade,
        };
        EngineIOAdapter.Subscribe(this);
        var engineIOMessageAdapter = _engineIOMessageAdapterFactory.Create(newValue.EngineIO);
        _serializer.SetEngineIOMessageAdapter(engineIOMessageAdapter);
        _serializer.Namespace = newValue.Namespace;
    }

    private EngineIOCompatibility GetEngineIOCompatibility(SessionOptions options)
    {
        if (Protocol == TransportProtocol.Polling)
        {
            return options.EngineIO == EngineIO.V3
                ? EngineIOCompatibility.HttpEngineIO3
                : EngineIOCompatibility.HttpEngineIO4;
        }

        return options.EngineIO == EngineIO.V3
            ? EngineIOCompatibility.WebSocketEngineIO3
            : EngineIOCompatibility.WebSocketEngineIO4;
    }

    public abstract Task SendAsync(object[] data, CancellationToken cancellationToken);

    public abstract Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);

    public abstract Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken);

    protected abstract Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken);

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (Options.ExtraHeaders is not null)
        {
            foreach (var header in Options.ExtraHeaders)
            {
                _protocolAdapter.SetDefaultHeader(header.Key, header.Value);
            }
        }

        try
        {
            var uri = GetServerUri();
            await ConnectCoreAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new ConnectionFailedException(e);
        }
    }

    private Uri GetServerUri()
    {
        var uriBuilder = GetUriBuilder();
        uriBuilder.Query = GetQueryString();
        return uriBuilder.Uri;
    }

    private UriBuilder GetUriBuilder()
    {
        return new UriBuilder
        {
            Scheme = GetServerUriSchema(),
            Port = Options.ServerUri.IsDefaultPort ? -1 : Options.ServerUri.Port,
            Path = string.IsNullOrWhiteSpace(Options.Path) ? DefaultPath : Options.Path!,
            Host = Options.ServerUri.Host
        };
    }

    private string GetQueryString()
    {
        var builder = new StringBuilder();
        builder.Append("EIO=").Append((int)Options.EngineIO);

        var query = GetProtocolQueries();
        foreach (string key in query)
        {
            builder.Append('&').Append(key).Append('=').Append(query[key]);
        }

        if (Options.Query != null)
        {
            foreach (string key in Options.Query)
            {
                builder.Append('&')
                    .Append(WebUtility.UrlEncode(key))
                    .Append('=')
                    .Append(WebUtility.UrlEncode(Options.Query[key]));
            }
        }

        return builder.ToString();
    }

    protected abstract string GetServerUriSchema();
    protected abstract NameValueCollection GetProtocolQueries();

    public abstract Task DisconnectAsync(CancellationToken cancellationToken);

    protected async Task HandleMessageAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            await OnNextBytesMessage(message.Bytes!).ConfigureAwait(false);
        }
        else
        {
            await OnNextTextMessage(message.Text!).ConfigureAwait(false);
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

        var shouldSwallow = await EngineIOAdapter.ProcessMessageAsync(message).ConfigureAwait(false);
        if (shouldSwallow)
        {
            return;
        }

        await OnNextAsync(message).ConfigureAwait(false);
    }

    protected virtual void OnOpenedMessage(OpenedMessage message)
    {
    }
}