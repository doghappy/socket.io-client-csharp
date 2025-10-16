using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core.Messages;
using SocketIOClient.Extensions;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Session;
using IHttpClient = SocketIOClient.Transport.Http.IHttpClient;

namespace SocketIOClient.V2;

public class SocketIO : ISocketIO, IInternalSocketIO
{
    public SocketIO(Uri uri, SocketIOOptions options, Action<IServiceCollection> configure = null)
    {
        _serverUri = uri;
        Options = options;
        _serviceProvider = ServicesInitializer.BuildServiceProvider(_services, configure);
        _logger = _serviceProvider.GetRequiredService<ILogger<SocketIO>>();
        _random = _serviceProvider.GetRequiredService<IRandom>();
    }

    public SocketIO(Uri uri, Action<IServiceCollection> configure) : this(uri, new SocketIOOptions(), configure)
    {
    }

    public SocketIO(Uri uri) : this(uri, new SocketIOOptions())
    {
    }

    public SocketIO(string uri) : this(new Uri(uri), new SocketIOOptions())
    {
    }

    public SocketIO(string uri, Action<IServiceCollection> configure) : this(uri, new SocketIOOptions(), configure)
    {
    }

    public SocketIO(string uri, SocketIOOptions options, Action<IServiceCollection> configure = null) : this(new Uri(uri), options, configure)
    {
    }

    private readonly ServiceCollection _services = new();

    public IHttpClient HttpClient { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SocketIO> _logger;
    private readonly IRandom _random;

    private ISession _session;
    private IServiceScope _scope;
    public int PacketId { get; private set; }
    public bool Connected { get; private set; }
    public string Id { get; private set; }

    private string Namespace { get; set; }

    private Uri _serverUri;

    private Uri ServerUri
    {
        get => _serverUri;
        set
        {
            if (_serverUri != value)
            {
                _serverUri = value;
                if (value != null && value.AbsolutePath != "/")
                {
                    Namespace = value.AbsolutePath;
                }
            }
        }
    }


    private readonly Dictionary<int, Action<IDataMessage>> _ackHandlers = new();
    private readonly Dictionary<int, Func<IDataMessage, Task>> _funcHandlers = new();
    private readonly Dictionary<string, Action<IAckableMessage>> _eventActionHandlers = new();
    private readonly Dictionary<string, Func<IAckableMessage, Task>> _eventFuncHandlers = new();

    // private TaskCompletionSource<bool> _openedCompletionSource = new();
    private TaskCompletionSource<bool> _sessionCompletionSource;
    private TaskCompletionSource<Exception> _connCompletionSource;
    public SocketIOOptions Options { get; }
    public event EventHandler<Exception> OnReconnectError;
    public event EventHandler OnPing;
    public event EventHandler<TimeSpan> OnPong;
    public event EventHandler OnConnected;
    public event EventHandler<string> OnDisconnected;
    public event EventHandler<string> OnError;
    public event EventHandler<int> OnReconnectAttempt;

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
        // TODO: dispose session
        _connCompletionSource = new TaskCompletionSource<Exception>();
        _sessionCompletionSource = new TaskCompletionSource<bool>();
        cancellationToken.Register(() => _connCompletionSource.SetResult(new TaskCanceledException()));
        _ = ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Waiting for _connCompletionSource...");
        var task = Task.Run(async () => await _connCompletionSource.Task.ConfigureAwait(false), cancellationToken);
        var ex = await task.ConfigureAwait(false);
        _logger.LogDebug("_connCompletionSource is done");
        if (ex != null)
        {
            throw ex;
        }
    }

    private async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        var attempts = Options.Reconnection ? Options.ReconnectionAttempts : 1;
        for (int i = 0; i < attempts; i++)
        {
            ReconnectAttempt(i, attempts);
            // TODO: dispose old session
            var session = NewSessionWithCancellationToken(cancellationToken);

            using var cts = new CancellationTokenSource(Options.ConnectionTimeout);
            try
            {
                await TryConnectAsync(session, cts).ConfigureAwait(false);
                break;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, e.Message);
                session.Dispose();
                var ex = new ConnectionException($"Cannot connect to server '{ServerUri}'", e);
                OnReconnectError.RunInBackground(this, ex);
                if (i == attempts - 1)
                {
                    _connCompletionSource.SetResult(ex);
                    throw ex;
                }
                var delay = _random.Next(Options.ReconnectionDelayMax);
                await Task.Delay(delay, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private void ReconnectAttempt(int i, int attempts)
    {
        var times = i + 1;
        _logger.LogDebug("ConnectCoreAsync attempt {Progress} / {Total}", times, attempts);
        OnReconnectAttempt.RunInBackground(this, times);
    }

    private async Task TryConnectAsync(ISession session, CancellationTokenSource cts)
    {
        session.Subscribe(this);
        await session.ConnectAsync(cts.Token).ConfigureAwait(false);
        _session = session;
        _sessionCompletionSource.SetResult(true);
        _logger.LogDebug("Set _sessionCompletionSource to true");
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
            _connCompletionSource.SetResult(ex);
            throw;
        }
        return session;
    }

    private ISession NewSession()
    {
        // TODO: dispose old scope
        _scope = _serviceProvider.CreateScope();
        _logger.LogDebug("Creating session...");
        var session = _scope.ServiceProvider.GetRequiredService<ISession>();
        session.Options = new SessionOptions
        {
            ServerUri = ServerUri,
            Path = Options.Path,
            Query = Options.Query,
            Timeout = Options.ConnectionTimeout,
            EngineIO = Options.EIO,
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
        await _session.SendAsync(sessionData, cancellationToken).ConfigureAwait(false);
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

    #region Emit action ack

    public async Task EmitAsync(string eventName, Action<IDataMessage> ack, CancellationToken cancellationToken)
    {
        await EmitAsync(eventName, [], ack, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, Action<IDataMessage> ack)
    {
        await EmitAsync(eventName, ack, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task EmitAsync(
        string eventName,
        IEnumerable<object> data,
        Action<IDataMessage> ack,
        CancellationToken cancellationToken)
    {
        CheckStatusAndData(data);
        PacketId++;
        var sessionData = MergeEventData(eventName, data);
        _ackHandlers.Add(PacketId, ack);
        await _session.SendAsync(sessionData, PacketId, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, IEnumerable<object> data, Action<IDataMessage> ack)
    {
        await EmitAsync(eventName, data, ack, CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    #region Emit func ack

    public async Task EmitAsync(
        string eventName,
        IEnumerable<object> data,
        Func<IDataMessage, Task> ack,
        CancellationToken cancellationToken)
    {
        CheckStatusAndData(data);
        PacketId++;
        var sessionData = MergeEventData(eventName, data);
        await _session.SendAsync(sessionData, PacketId, cancellationToken).ConfigureAwait(false);
        _funcHandlers.Add(PacketId, ack);
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

    async Task IInternalSocketIO.SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken)
    {
        await SendAckDataAsync(packetId, data, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken)
    {
        CheckStatusAndData(data);
        await _session.SendAckDataAsync(data.ToArray(), packetId, cancellationToken).ConfigureAwait(false);
    }

    async Task IMyObserver<IMessage>.OnNextAsync(IMessage message)
    {
        await OnNextAsync(message).ConfigureAwait(false);
    }

    private async Task OnNextAsync(IMessage message)
    {
        switch (message.Type)
        {
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
                await HandleDisconnectedMessageAsync().ConfigureAwait(false);
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

    private IAckableMessage ToAckableMessage(IDataMessage message)
    {
        return new AckableMessage(message, this);
    }

    private async Task HandleEventMessage(IMessage message)
    {
        var eventMessage = (IEventMessage)message;
        if (_eventActionHandlers.TryGetValue(eventMessage.Event, out var actionHandler))
        {
            actionHandler(ToAckableMessage(eventMessage));
            return;
        }
        if (_eventFuncHandlers.TryGetValue(eventMessage.Event, out var funcHandler))
        {
            await funcHandler(ToAckableMessage(eventMessage));
        }
    }

    private async Task HandleAckMessage(IMessage message)
    {
        var ackMessage = (IDataMessage)message;
        if (_ackHandlers.TryGetValue(ackMessage.Id, out var ack))
        {
            ack(ackMessage);
        }
        else if (_funcHandlers.TryGetValue(ackMessage.Id, out var func))
        {
            await func(ackMessage);
        }
    }

    private async Task HandleConnectedMessage(IMessage message)
    {
        await _sessionCompletionSource.Task.ConfigureAwait(false);
        var connectedMessage = (ConnectedMessage)message;
        Id = connectedMessage.Sid;
        Connected = true;
        _ = Task.Run(() => OnConnected?.Invoke(this, EventArgs.Empty));
        _connCompletionSource.SetResult(null);
    }

    private async Task HandleDisconnectedMessageAsync()
    {
        OnDisconnected?.Invoke(this, DisconnectReason.IOServerDisconnect);
        await DisconnectCoreAsync(CancellationToken.None);
    }

    private void HandlePongMessage(IMessage message)
    {
        var pong = (PongMessage)message;
        OnPong?.Invoke(this, pong.Duration);
    }

    private void HandleErrorMessage(IMessage message)
    {
        var err = (ErrorMessage)message;
        _connCompletionSource.SetResult(new ConnectionException(err.Error));
        OnError?.Invoke(this, err.Error);
    }

    public async Task DisconnectAsync()
    {
        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await DisconnectCoreAsync(cancellationToken).ConfigureAwait(false);
        OnDisconnected?.Invoke(this, DisconnectReason.IOClientDisconnect);
    }

    private async Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            try
            {
                await _session.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            _session.Dispose();
        }
        Connected = false;
        Id = null;
    }

    public void On(string eventName, Action<IAckableMessage> handler)
    {
        ThrowIfInvalidEventHandler(eventName, handler);
        if (_eventFuncHandlers.ContainsKey(eventName))
        {
            throw new ArgumentException("A handler with the same event name already exists");
        }
        if (_eventActionHandlers.ContainsKey(eventName))
        {
            return;
        }
        _eventActionHandlers.Add(eventName, handler);
    }

    public void On(string eventName, Func<IAckableMessage, Task> handler)
    {
        ThrowIfInvalidEventHandler(eventName, handler);
        if (_eventActionHandlers.ContainsKey(eventName))
        {
            throw new ArgumentException("A handler with the same event name already exists");
        }
        if (_eventFuncHandlers.ContainsKey(eventName))
        {
            return;
        }
        _eventFuncHandlers.Add(eventName, handler);
    }

    private static void ThrowIfInvalidEventHandler(string eventName, object handler)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentException("Invalid event name", nameof(eventName));
        }
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
    }
}