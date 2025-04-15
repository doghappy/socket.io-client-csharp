using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;
using IHttpClient = SocketIOClient.Transport.Http.IHttpClient;

namespace SocketIOClient.V2;

public class SocketIO : ISocketIO
{
    public IHttpClient HttpClient { get; set; }
    public ISessionFactory SessionFactory { get; set; }
    private ISession _session;
    public int PacketId { get; private set; }
    public bool Connected { get; private set; }


    private readonly Dictionary<int, Action<IAckMessage>> _ackHandlers = new();
    private readonly Dictionary<int, Func<IAckMessage, Task>> _funcHandlers = new();
    private readonly SocketIOOptions _options;


    public SocketIO(Uri uri, SocketIOOptions options)
    {
        _options = options;
        SessionFactory = new DefaultSessionFactory();
    }

    public SocketIO(Uri uri) : this(uri, new SocketIOOptions())
    {
    }

    public SocketIO(string uri) : this(new Uri(uri), new SocketIOOptions())
    {
    }

    public Task ConnectAsync()
    {
        _session = SessionFactory.New(_options.EIO);
        // Session.Subscribe(this);
        Connected = true;
        return Task.CompletedTask;
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
        if (message.Type == MessageType.Ack)
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
    }
}