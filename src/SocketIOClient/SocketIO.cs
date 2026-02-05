using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Exceptions;
using SocketIOClient.Extensions;
using SocketIOClient.Infrastructure;
using SocketIOClient.Observers;
using SocketIOClient.Session;

namespace SocketIOClient;

public class SocketIO : ISocketIO, IInternalSocketIO
{
    public SocketIO(Uri uri, SocketIOOptions options, Action<IServiceCollection>? configure = null)
    {
        ServerUri = uri;
        Options = options;
        _serviceProvider = ServicesInitializer.BuildServiceProvider(_services, configure);
        _logger = _serviceProvider.GetRequiredService<ILogger<SocketIO>>();
        _random = _serviceProvider.GetRequiredService<IRandom>();
        _delay = _serviceProvider.GetRequiredService<IDelay>();
    }

    public SocketIO(Uri uri, Action<IServiceCollection> configure) : this(uri, new SocketIOOptions(), configure)
    {
    }

    public SocketIO(Uri uri) : this(uri, new SocketIOOptions())
    {
    }

    private readonly ServiceCollection _services = [];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SocketIO> _logger;
    private readonly IRandom _random;
    private readonly IDelay _delay;
    private readonly object _disconnectLock = new();
    private readonly object _ackHandlerLock = new();

    private ISession? _session;
    private IServiceScope? _scope;

    private int _packetId;
    int IInternalSocketIO.PacketId => _packetId;

    public bool Connected { get; private set; }
    public string? Id { get; private set; }

    private string? _namespace;

    private Uri _serverUri = null!;

    private Uri ServerUri
    {
        get => _serverUri;
        set
        {
            if (_serverUri == value) return;
            _serverUri = value;
            if (value.AbsolutePath != "/")
            {
                _namespace = value.AbsolutePath.TrimEnd('/');
            }
        }
    }

    int IInternalSocketIO.AckHandlerCount => _ackHandlers.Count;
    private readonly Dictionary<int, Func<IDataMessage, Task>> _ackHandlers = new();
    private readonly Dictionary<string, Func<IEventContext, Task>> _eventHandlers = new();
    private readonly HashSet<string> _onceEvents = [];
    private readonly List<Func<string, IEventContext, Task>> _onAnyHandlers = [];

    private TaskCompletionSource<bool>? _sessionCompletionSource;
    private TaskCompletionSource<Exception?>? _connCompletionSource;
    public SocketIOOptions Options { get; }
    public event EventHandler<Exception>? OnReconnectError;
    public event EventHandler? OnPing;
    public event EventHandler<TimeSpan>? OnPong;
    public event EventHandler? OnConnected;
    public event EventHandler<string>? OnDisconnected;
    public event EventHandler<string>? OnError;
    public event EventHandler<int>? OnReconnectAttempt;

    public async Task ConnectAsync()
    {
        await ConnectAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (Connected)
        {
            return;
        }

        _connCompletionSource = new TaskCompletionSource<Exception?>();
        _sessionCompletionSource = new TaskCompletionSource<bool>();
        var timeout = (int)(Options.ConnectionTimeout.TotalMilliseconds * 1.02);
        if (Options.Reconnection)
        {
            timeout *= Options.ReconnectionAttempts;
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        timeoutCts.Token.Register(() => _connCompletionSource.SetResult(new TimeoutException()));

        cancellationToken.Register(() => _connCompletionSource.SetResult(new TaskCanceledException()));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var ctsToken = cts.Token;

        _ = ConnectCoreAsync(ctsToken).ConfigureAwait(false);
        _logger.LogDebug("Waiting for socket.io connection result...");
        var task = Task.Run(async () => await _connCompletionSource.Task.ConfigureAwait(false), ctsToken);
        var ex = await task.ConfigureAwait(false);
        _logger.LogDebug("Got socket.io connection result");
        if (ex != null)
        {
            _logger.LogDebug(ex.ToString());
            throw ex;
        }
    }

    private async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        var attempts = Options.Reconnection ? Options.ReconnectionAttempts : 1;
        for (int i = 0; i < attempts; i++)
        {
            ReconnectAttempt(i, attempts);
            var session = NewSessionWithCancellationToken(cancellationToken);

            using var cts = new CancellationTokenSource(Options.ConnectionTimeout);
            var timeoutCancellationToken = cts.Token;
            try
            {
                await TryConnectAsync(session, timeoutCancellationToken).ConfigureAwait(false);
                break;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, e.Message);
                var ex = new ConnectionException($"Cannot connect to server '{ServerUri}'", e);
                OnReconnectError?.RunInBackground(this, ex);
                if (i == attempts - 1)
                {
                    _connCompletionSource!.SetResult(ex);
                    throw ex;
                }

                var delay = _random.Next(Options.ReconnectionDelayMax);
                await _delay.DelayAsync(delay, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private void ReconnectAttempt(int i, int attempts)
    {
        var times = i + 1;
        _logger.LogDebug("ConnectCoreAsync attempt {Progress} / {Total}", times, attempts);
        OnReconnectAttempt?.RunInBackground(this, times);
    }

    private async Task TryConnectAsync(ISession session, CancellationToken cancellationToken)
    {
        session.Subscribe(this);
        _logger.LogDebug("Session connecting...");
        await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
        _session = session;
        _sessionCompletionSource!.SetResult(true);
        _logger.LogDebug("Session connected");
    }

    private ISession NewSessionWithCancellationToken(CancellationToken cancellationToken)
    {
        ISession session;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            session = NewSession();
        }
        catch (Exception ex)
        {
            _connCompletionSource!.SetResult(ex);
            throw;
        }

        return session;
    }

    private ISession NewSession()
    {
        _scope?.Dispose();
        _scope = _serviceProvider.CreateScope();
        _logger.LogDebug("Creating session...");
        var session = _scope.ServiceProvider.GetRequiredKeyedService<ISession>(Options.Transport);
        session.Options = new SessionOptions
        {
            ServerUri = ServerUri,
            Path = Options.Path,
            Query = Options.Query,
            Timeout = Options.ConnectionTimeout,
            EngineIO = Options.EIO,
            ExtraHeaders = Options.ExtraHeaders,
            Namespace = _namespace,
            Auth = Options.Auth,
            AutoUpgrade = Options.AutoUpgrade,
        };
        _logger.LogDebug("Session created: {Type}", session.GetType().Name);
        return session;
    }

    private void ThrowIfNotConnected()
    {
        if (Connected)
        {
            return;
        }

        throw new InvalidOperationException("SocketIO is not connected.");
    }

    private static void ThrowIfDataIsNull(object data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
    }

    private void CheckStatusAndData(object data)
    {
        ThrowIfNotConnected();
        ThrowIfDataIsNull(data);
    }

    private static object[] MergeEventData(string eventName, IEnumerable<object> data)
    {
        return new[] { eventName }.Concat(data).ToArray();
    }

    #region Emit event

    public async Task EmitAsync(string eventName, IEnumerable<object> data, CancellationToken cancellationToken)
    {
        CheckStatusAndData(data);
        var sessionData = MergeEventData(eventName, data);
        await _session!.SendAsync(sessionData, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, IEnumerable<object> data)
    {
        await EmitAsync(eventName, data, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, CancellationToken cancellationToken)
    {
        await EmitAsync(eventName, [], cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName)
    {
        await EmitAsync(eventName, CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    #region Emit ack

    public async Task EmitAsync(
        string eventName,
        IEnumerable<object> data,
        Func<IDataMessage, Task> ack,
        CancellationToken cancellationToken)
    {
        CheckStatusAndData(data);
        lock (_ackHandlerLock)
        {
            _packetId++;
            _ackHandlers.Add(_packetId, ack);
        }
        var sessionData = MergeEventData(eventName, data);
        await _session!.SendAsync(sessionData, _packetId, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, IEnumerable<object> data, Func<IDataMessage, Task> ack)
    {
        await EmitAsync(eventName, data, ack, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, Func<IDataMessage, Task> ack, CancellationToken cancellationToken)
    {
        await EmitAsync(eventName, [], ack, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, Func<IDataMessage, Task> ack)
    {
        await EmitAsync(eventName, ack, CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    async Task IInternalSocketIO.SendAckDataAsync(int packetId, IEnumerable<object> data)
    {
        await SendAckDataAsync(packetId, data, CancellationToken.None).ConfigureAwait(false);
    }

    async Task IInternalSocketIO.SendAckDataAsync(int packetId, IEnumerable<object> data,
        CancellationToken cancellationToken)
    {
        await SendAckDataAsync(packetId, data, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken)
    {
        CheckStatusAndData(data);
        await _session!.SendAckDataAsync(data.ToArray(), packetId, cancellationToken).ConfigureAwait(false);
    }

    async Task IMyObserver<IMessage>.OnNextAsync(IMessage message)
    {
        await OnNextAsync(message).ConfigureAwait(false);
    }

    private async Task OnNextAsync(IMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Opened:
                await HandleOpenedMessage(message).ConfigureAwait(false);
                break;
            case MessageType.Ping:
                OnPing?.Invoke(this, EventArgs.Empty);
                break;
            case MessageType.Pong:
                HandlePongMessage(message);
                break;
            case MessageType.Connected:
                await HandleConnectedMessage(message).ConfigureAwait(false);
                break;
            case MessageType.Disconnected:
                InvokeOnDisconnected(DisconnectReason.IOServerDisconnect);
                break;
            case MessageType.Event:
            case MessageType.Binary:
                await HandleEventMessage(message).ConfigureAwait(false);
                break;
            case MessageType.Ack:
            case MessageType.BinaryAck:
                await HandleAckMessage(message).ConfigureAwait(false);
                break;
            case MessageType.Error:
                HandleErrorMessage(message);
                break;
        }
    }

    private async Task HandleOpenedMessage(IMessage message)
    {
        if (!Options.AutoUpgrade)
        {
            return;
        }

        var openedMessage = (OpenedMessage)message;
        if (!openedMessage.Upgrades.Contains("websocket"))
        {
            return;
        }

        Options.Transport = TransportProtocol.WebSocket;
        await UpgradeTransportAsync(openedMessage).ConfigureAwait(false);
    }

    private async Task UpgradeTransportAsync(OpenedMessage message)
    {
        _logger.LogDebug("Transport upgrading...");
        using var timeoutCts = new CancellationTokenSource(Options.ConnectionTimeout);
        timeoutCts.Token.Register(() => _connCompletionSource!.SetResult(new TimeoutException()));
        var cancellationToken = timeoutCts.Token;
        var session = NewSessionWithCancellationToken(cancellationToken);
        session.Options.Sid = message.Sid;
        _sessionCompletionSource = new TaskCompletionSource<bool>();
        await TryConnectAsync(session, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Transport upgraded");
    }

    private async Task HandleEventMessage(IMessage message)
    {
        var eventMessage = (IEventMessage)message;
        var ctx = new EventContext(eventMessage, this);

        await InvokeOnAnyHandlersAsync(eventMessage, ctx).ConfigureAwait(false);
        await InvokeEventHandlersAsync(eventMessage, ctx).ConfigureAwait(false);
    }

    private async Task InvokeEventHandlersAsync(IEventMessage eventMessage, IEventContext ctx)
    {
        if (_eventHandlers.TryGetValue(eventMessage.Event, out var eventHandler))
        {
            var isOnce = _onceEvents.Contains(eventMessage.Event);
            if (isOnce)
            {
                _eventHandlers.Remove(eventMessage.Event);
            }

            try
            {
                await eventHandler(ctx).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured in the event handler, event name: '{Event}'", eventMessage.Event);
            }
        }
    }

    private async Task InvokeOnAnyHandlersAsync(IEventMessage eventMessage, IEventContext ctx)
    {
        foreach (var handler in _onAnyHandlers)
        {
            try
            {
                await handler(eventMessage.Event, ctx).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured in one of the OnAny handlers");
            }
        }
    }

    private async Task HandleAckMessage(IMessage message)
    {
        var ackMessage = (IDataMessage)message;
        Func<IDataMessage, Task>? handler;
        lock (_ackHandlerLock)
        {
            if (_ackHandlers.TryGetValue(ackMessage.Id, out handler))
            {
                _ackHandlers.Remove(ackMessage.Id);
            }
        }

        if (handler != null)
        {
            await handler(ackMessage).ConfigureAwait(false);
        }
    }

    private async Task HandleConnectedMessage(IMessage message)
    {
        await _sessionCompletionSource!.Task.ConfigureAwait(false);
        var connectedMessage = (ConnectedMessage)message;
        Id = connectedMessage.Sid;
        Connected = true;
        _ = Task.Run(() => OnConnected?.Invoke(this, EventArgs.Empty));
        _connCompletionSource!.SetResult(null);
        _session!.OnDisconnected = () => InvokeOnDisconnected(DisconnectReason.TransportError);
    }

    private void InvokeOnDisconnected(string reason)
    {
        lock (_disconnectLock)
        {
            if (!Connected)
            {
                return;
            }

            _session?.Unsubscribe(this);
            ResetStatusForDisconnected();
            OnDisconnected?.Invoke(this, reason);
        }
    }

    private void HandlePongMessage(IMessage message)
    {
        var pong = (PongMessage)message;
        OnPong?.Invoke(this, pong.Duration);
    }

    private void HandleErrorMessage(IMessage message)
    {
        var err = (ErrorMessage)message;
        _connCompletionSource!.SetResult(new ConnectionException(err.Error));
        OnError?.Invoke(this, err.Error);
    }

    public async Task DisconnectAsync()
    {
        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await TrySendDisconnectMessageAsync(cancellationToken).ConfigureAwait(false);
        InvokeOnDisconnected(DisconnectReason.IOClientDisconnect);
    }

    private async Task TrySendDisconnectMessageAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            try
            {
                await _session.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }
    }

    private void ResetStatusForDisconnected()
    {
        Connected = false;
        Id = null;
        _packetId = 0;
        _ackHandlers.Clear();
    }

    public void On(string eventName, Func<IEventContext, Task> handler)
    {
        ThrowIfInvalidEventNameHandler(eventName, handler);
        _eventHandlers[eventName] = handler;
        _onceEvents.Remove(eventName);
    }

    private static void ThrowIfInvalidEventNameHandler(string eventName, Func<IEventContext, Task> handler)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentException("Invalid event name", nameof(eventName));
        }

        if ((object)handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
    }

    public void OnAny(Func<string, IEventContext, Task> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _onAnyHandlers.Add(handler);
    }

    public IEnumerable<Func<string, IEventContext, Task>> ListenersAny => _onAnyHandlers;

    public void OffAny(Func<string, IEventContext, Task> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _onAnyHandlers.Remove(handler);
    }

    public void PrependAny(Func<string, IEventContext, Task> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _onAnyHandlers.Insert(0, handler);
    }

    public void Once(string eventName, Func<IEventContext, Task> handler)
    {
        On(eventName, handler);
        _onceEvents.Add(eventName);
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}