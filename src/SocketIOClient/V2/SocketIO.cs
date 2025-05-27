using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Session;
using IHttpClient = SocketIOClient.Transport.Http.IHttpClient;

namespace SocketIOClient.V2;

public class SocketIO : ISocketIO
{
    public SocketIO(Uri uri, SocketIOOptions options)
    {
        _serverUri = uri;
        Options = options;
        SessionFactory = new DefaultSessionFactory();
        Random = new SystemRandom();
    }

    public SocketIO(Uri uri) : this(uri, new SocketIOOptions())
    {
    }

    public SocketIO(string uri) : this(new Uri(uri), new SocketIOOptions())
    {
    }

    public SocketIO(string uri, SocketIOOptions options) : this(new Uri(uri), options)
    {
    }

    public IHttpClient HttpClient { get; set; }
    public ISessionFactory SessionFactory { get; set; }
    private ISession _session;
    public int PacketId { get; private set; }
    public bool Connected { get; private set; }
    public string Id { get; private set; }
    public IRandom Random { get; set; }

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


    private readonly Dictionary<int, Action<IAckMessage>> _ackHandlers = new();
    private readonly Dictionary<int, Func<IAckMessage, Task>> _funcHandlers = new();
    private readonly Dictionary<string, Action<IAckMessage>> _eventActionHandlers = new();
    private readonly Dictionary<string, Func<IAckMessage, Task>> _eventFuncHandlers = new();

    // private TaskCompletionSource<bool> _openedCompletionSource = new();
    private TaskCompletionSource<bool> _sessionCompletionSource;
    private TaskCompletionSource<Exception> _connCompletionSource;
    public SocketIOOptions Options { get; }
    public event EventHandler<Exception> OnReconnectError;
    public event EventHandler OnPing;
    public event EventHandler<TimeSpan> OnPong;
    public event EventHandler OnConnected;

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
        var task = Task.Run(async () => await _connCompletionSource.Task.ConfigureAwait(false), cancellationToken);
        var ex = await task.ConfigureAwait(false);
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
            cancellationToken.ThrowIfCancellationRequested();
            var session = SessionFactory.New(Options.EIO, new SessionOptions
            {
                ServerUri = ServerUri,
                Path = Options.Path,
                Query = Options.Query,
                Timeout = Options.ConnectionTimeout,
            });
            using var cts = new CancellationTokenSource(Options.ConnectionTimeout);
            try
            {
                session.Subscribe(this);
                await session.ConnectAsync(cts.Token).ConfigureAwait(false);
                _session = session;
                _sessionCompletionSource.SetResult(true);
                break;
            }
            catch (Exception e)
            {
                session.Dispose();
                var ex = new ConnectionException($"Cannot connect to server '{ServerUri}'", e);
                OnReconnectError?.Invoke(this, ex);
                if (i == attempts - 1)
                {
                    _connCompletionSource.SetResult(ex);
                    throw ex;
                }
                var delay = Random.Next(Options.ReconnectionDelayMax);
                await Task.Delay(delay, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private void ThrowIfNotConnected()
    {
        if (Connected)
        {
            return;
        }
        throw new InvalidOperationException("SocketIO is not connected.");
    }

    private static void ThrowIfDataIsNull(IEnumerable<object> data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }
    }

    private static object[] MergeEventData(string eventName, IEnumerable<object> data)
    {
        return new[] { eventName }.Concat(data).ToArray();
    }

    #region Emit event

    public async Task EmitAsync(string eventName, IEnumerable<object> data, CancellationToken cancellationToken)
    {
        ThrowIfNotConnected();
        // ReSharper disable PossibleMultipleEnumeration
        ThrowIfDataIsNull(data);
        var sessionData = MergeEventData(eventName, data);
        // ReSharper restore PossibleMultipleEnumeration
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

    public async Task EmitAsync(string eventName, Action<IAckMessage> ack, CancellationToken cancellationToken)
    {
        await EmitAsync(eventName, [], ack, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, Action<IAckMessage> ack)
    {
        await EmitAsync(eventName, ack, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task EmitAsync(
        string eventName,
        IEnumerable<object> data,
        Action<IAckMessage> ack,
        CancellationToken cancellationToken)
    {
        ThrowIfNotConnected();
        PacketId++;
        var sessionData = MergeEventData(eventName, data);
        _ackHandlers.Add(PacketId, ack);
        await _session.SendAsync(sessionData, PacketId, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, IEnumerable<object> data, Action<IAckMessage> ack)
    {
        await EmitAsync(eventName, data, ack, CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    #region Emit func ack

    public async Task EmitAsync(
        string eventName,
        IEnumerable<object> data,
        Func<IAckMessage, Task> ack,
        CancellationToken cancellationToken)
    {
        ThrowIfNotConnected();
        PacketId++;
        var sessionData = MergeEventData(eventName, data);
        await _session.SendAsync(sessionData, PacketId, cancellationToken).ConfigureAwait(false);
        _funcHandlers.Add(PacketId, ack);
    }

    public async Task EmitAsync(string eventName, IEnumerable<object> data, Func<IAckMessage, Task> ack)
    {
        await EmitAsync(eventName, data, ack, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, Func<IAckMessage, Task> ack, CancellationToken cancellationToken)
    {
        await EmitAsync(eventName, [], ack, cancellationToken).ConfigureAwait(false);
    }

    public async Task EmitAsync(string eventName, Func<IAckMessage, Task> ack)
    {
        await EmitAsync(eventName, ack, CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    public async Task OnNextAsync(IMessage message)
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
                await HandleConnectedMessage(message);
                break;
            case MessageType.Event:
                await HandleEventMessage(message);
                break;
            case MessageType.Ack:
                await HandleAckMessage(message);
                break;
        }
    }

    private async Task HandleEventMessage(IMessage message)
    {
        var eventMessage = (IEventMessage)message;
        if (_eventActionHandlers.TryGetValue(eventMessage.Event, out var actionHandler))
        {
            actionHandler(eventMessage);
            return;
        }
        if (_eventFuncHandlers.TryGetValue(eventMessage.Event, out var funcHandler))
        {
            await funcHandler(eventMessage);
        }
    }

    private async Task HandleAckMessage(IMessage message)
    {
        var ackMessage = (IAckMessage)message;
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

    private void HandlePongMessage(IMessage message)
    {
        var pong = (PongMessage)message;
        OnPong?.Invoke(this, pong.Duration);
    }

    public Task DisconnectAsync()
    {
        // await _session.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        _session?.Dispose();
        Connected = false;
        Id = null;
        return Task.CompletedTask;
    }

    public void On(string eventName, Action<IAckMessage> handler)
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

    public void On(string eventName, Func<IAckMessage, Task> handler)
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