using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    // private TaskCompletionSource<bool> _openedCompletionSource = new();
    private TaskCompletionSource<bool> _sessionCompletionSource;
    private TaskCompletionSource<Exception> _connCompletionSource;
    public SocketIOOptions Options { get; }
    public event EventHandler<Exception> OnReconnectError;
    public event EventHandler OnPing;
    public event EventHandler<TimeSpan> OnPong;

    public async Task ConnectAsync()
    {
        await ConnectAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        // TODO: concurrent connect
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
                await session.ConnectAsync(cts.Token).ConfigureAwait(false);
                _session = session;
                _session.Subscribe(this);
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


    // public Task EmitAsync(string eventName, Action ack)
    // {
    //     throw new NotImplementedException();
    // }

    private void ThrowIfNotConnected()
    {
        if (Connected)
        {
            return;
        }
        throw new InvalidOperationException("SocketIO is not connected.");
    }

    public async Task EmitAsync(string eventName, Action<IAckMessage> ack)
    {
        ThrowIfNotConnected();
        PacketId++;
        await _session.SendAsync([eventName], CancellationToken.None);
        _ackHandlers.Add(PacketId, ack);
    }

    public async Task EmitAsync(string eventName, Func<IAckMessage, Task> ack)
    {
        ThrowIfNotConnected();
        PacketId++;
        await _session.SendAsync([eventName], CancellationToken.None);
        _funcHandlers.Add(PacketId, ack);
    }

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
            case MessageType.Ack:
                await HandleAckMessage(message);
                break;
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
}