using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;
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
    private readonly Queue<IBinaryMessage> _messageQueue = [];

    public int PendingDeliveryCount => _messageQueue.Count;

    public void OnNext(ProtocolMessage protocolMessage)
    {
        if (protocolMessage.Type == ProtocolMessageType.Bytes)
        {
            OnNextBytesMessage(protocolMessage.Bytes);
            return;
        }
        var messages = _engineIOAdapter.GetMessages(protocolMessage.Text);
        HandleMessages(messages);
    }

    private void HandleMessages(IEnumerable<ProtocolMessage> messages)
    {
        foreach (var message in messages)
        {
            if (message.Type == ProtocolMessageType.Bytes)
            {
                OnNextBytesMessage(message.Bytes);
            }
            else
            {
                OnNextTextMessage(message.Text);
            }
        }
    }

    private void OnNextTextMessage(string text)
    {
        var message = _serializer.Deserialize(text);
        if (message is null)
        {
            return;
        }
        if (message.Type is MessageType.Binary or MessageType.BinaryAck)
        {
            _messageQueue.Enqueue((IBinaryMessage)message);
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

    public async Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data);
        foreach (var message in messages)
        {
            await _httpAdapter.SendAsync(message, cancellationToken);
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var uri = _uriConverter.GetServerUri(
            false,
            SessionOptions.ServerUri,
            SessionOptions.Path,
            SessionOptions.Query);
        var req = new HttpRequest
        {
            Uri = uri,
        };
        try
        {
            await _httpAdapter.SendAsync(req, cancellationToken);
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