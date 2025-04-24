using System;
using System.Collections.Generic;
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
    public SocketIOOptions Options { get; }
    public event EventHandler<Exception> OnReconnectError;
    public event EventHandler OnPing;
    public event EventHandler<TimeSpan> OnPong;

    public async Task ConnectAsync()
    {
        await ConnectAsync(CancellationToken.None);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var attempts = Options.Reconnection ? Options.ReconnectionAttempts : 1;
        for (int i = 0; i < attempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // TODO: IDisposable
            var session = SessionFactory.New(Options.EIO, new SessionOptions
            {

            });
            using var cts = new CancellationTokenSource(Options.ConnectionTimeout);
            try
            {
                await session.ConnectAsync(cts.Token);
                _session = session;
                _session.Subscribe(this);
            }
            catch (Exception e)
            {
                var ex = new ConnectionException($"Cannot connect to server '{ServerUri}'", e);
                OnReconnectError?.Invoke(this, ex);
                if (i == attempts - 1)
                {
                    throw ex;
                }
                var delay = Random.Next(Options.ReconnectionDelayMax);
                await Task.Delay(delay, CancellationToken.None);
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

    private Task HandleConnectedMessage(IMessage message)
    {
        var connectedMessage = (ConnectedMessage)message;
        Id = connectedMessage.Sid;
        Connected = true;
        return Task.CompletedTask;
    }
}