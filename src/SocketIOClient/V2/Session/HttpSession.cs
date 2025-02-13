using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session;

public class HttpSession : ISession
{
    public HttpSession(
        IEngineIOAdapter engineIOAdapter,
        IHttpAdapter httpAdapter,
        ISerializer serializer,
        IUriConverter uriConverter)
    {
        _engineIOAdapter = engineIOAdapter;
        _httpAdapter = httpAdapter;
        _serializer = serializer;
        _uriConverter = uriConverter;
        httpAdapter.Subscribe(this);
    }

    private readonly IEngineIOAdapter _engineIOAdapter;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;
    private readonly IUriConverter _uriConverter;
    private readonly List<IMyObserver<IMessage>> _observers = [];
    private readonly Queue<IBinaryEventMessage> _messageQueue = [];

    public int PendingDeliveryCount => _messageQueue.Count;

    public void OnNext(ProtocolMessage protocolMessage)
    {
        if (protocolMessage.Type == ProtocolMessageType.Text)
        {
            OnNextTextMessage(protocolMessage.Text);
            return;
        }
        OnNextBytesMessage(protocolMessage.Bytes);
    }

    private void OnNextTextMessage(string text)
    {
        var message = _serializer.Deserialize(text);
        if (message is null)
        {
            return;
        }
        if (message.Type == MessageType.Binary)
        {
            _messageQueue.Enqueue((IBinaryEventMessage)message);
            return;
        }
        NotifyObservers(message);
    }

    private void OnNextBytesMessage(byte[] bytes)
    {
        var message = _messageQueue.Peek();
        message.Add(bytes);
        if (message.ReadyDelivery)
        {
            _messageQueue.Dequeue();
            NotifyObservers(message);
        }
    }

    private void NotifyObservers(IMessage message)
    {
        foreach (var observer in _observers)
        {
            observer.OnNext(message);
        }
    }

    public SessionOptions SessionOptions { get; set; }

    public async Task SendAsync(IMessage message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }
        // TODO: serialize by message.Type
        // var protocolMessages = serializer.Serialize()
        await _httpAdapter.SendAsync(null, cancellationToken);
        throw new NotImplementedException();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var uri = _uriConverter.GetServerUri(
            false,
            SessionOptions.ServerUri,
            SessionOptions.Path,
            SessionOptions.Query);
        var message = new ProtocolMessage
        {
            Text = string.Empty,
        };
        try
        {
            await _httpAdapter.SendAsync(message, cancellationToken);
        }
        catch (Exception e)
        {
            throw new ConnectionFailedException(e);
        }
    }

    public void Subscribe(IMyObserver<IMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }
}