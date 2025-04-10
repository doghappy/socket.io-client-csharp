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
    private readonly Dictionary<int, Func<SocketIOResponse, Task>> _funcHandlers = new();
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

    private IEngineIOAdapter NewEnginIOAdapter()
    {
        if (_options.EIO == EngineIO.V3)
        {
            return new EngineIO3Adapter();
        }
        return new EngineIO4Adapter();
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

    public void OnNext(IMessage message)
    {
        if (message.Type == MessageType.Ack)
        {
            var ackMessage = (IAckMessage)message;
            _ackHandlers[ackMessage.Id](ackMessage);
        }
    }
}