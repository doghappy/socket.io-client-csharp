using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        SessionOptions options,
        IEngineIOAdapter engineIOAdapter,
        IHttpAdapter httpAdapter,
        ISerializer serializer,
        IUriConverter uriConverter)
    {
        _options = options;
        _engineIOAdapter = engineIOAdapter;
        _httpAdapter = httpAdapter;
        _serializer = serializer;
        _uriConverter = uriConverter;

        _engineIOAdapter.Subscribe(this);
        _httpAdapter.Subscribe(this);
    }

    private readonly SessionOptions _options;
    private readonly IEngineIOAdapter _engineIOAdapter;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;
    private readonly IUriConverter _uriConverter;
    private readonly List<IMyObserver<IMessage>> _observers = [];
    private readonly Queue<IBinaryMessage> _messageQueue = [];

    public int PendingDeliveryCount => _messageQueue.Count;

    public async Task OnNextAsync(ProtocolMessage protocolMessage)
    {
        if (protocolMessage.Type == ProtocolMessageType.Bytes)
        {
            await OnNextBytesMessage(protocolMessage.Bytes);
            return;
        }
        var messages = _engineIOAdapter.GetMessages(protocolMessage.Text);
        await HandleMessages(messages).ConfigureAwait(false);
    }

    private async Task HandleMessages(IEnumerable<ProtocolMessage> messages)
    {
        foreach (var message in messages)
        {
            if (message.Type == ProtocolMessageType.Bytes)
            {
                await OnNextBytesMessage(message.Bytes).ConfigureAwait(false);
            }
            else
            {
                await OnNextTextMessage(message.Text).ConfigureAwait(false);
            }
        }
    }

    private async Task OnNextTextMessage(string text)
    {
        Debug.WriteLine($"[Polling⬇] {text}");
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
        await _engineIOAdapter.ProcessMessageAsync(message).ConfigureAwait(false);
        if (message.Type is MessageType.Opened)
        {
            var openedMessage = (OpenedMessage)message;
            _httpAdapter.Uri = new Uri($"{_httpAdapter.Uri.AbsoluteUri}&sid={openedMessage.Sid}");
        }
        await OnNextAsync(message).ConfigureAwait(false);
    }

    private async Task OnNextBytesMessage(byte[] bytes)
    {
        Debug.WriteLine($"[Polling⬇] 0️⃣1️⃣0️⃣1️⃣ {bytes.Length}");
        var message = _messageQueue.Peek();
        message.Add(bytes);
        if (message.ReadyDelivery)
        {
            _messageQueue.Dequeue();
            await OnNextAsync(message).ConfigureAwait(false);
        }
    }

    public async Task OnNextAsync(IMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    // public Task OnNextAsync(IMessage message)
    // {
    //     foreach (var observer in _observers)
    //     {
    //         observer.OnNextAsync(message);
    //     }
    //     return Task.CompletedTask;
    // }

    public async Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendProtocolMessagesAsync(IEnumerable<ProtocolMessage> messages, CancellationToken cancellationToken)
    {
        var bytes = new List<byte[]>();
        foreach (var message in messages)
        {
            if (message.Type == ProtocolMessageType.Text)
            {
                var request = _engineIOAdapter.ToHttpRequest(message.Text);
#if DEBUG
                Debug.WriteLine($"[Polling⬆] {request.BodyText}");
#endif
                await _httpAdapter.SendAsync(request, cancellationToken).ConfigureAwait(false);
                continue;
            }
#if DEBUG
            Debug.WriteLine($"[Polling⬆] 0️⃣1️⃣0️⃣1️⃣ {message.Bytes.Length}");
#endif
            bytes.Add(message.Bytes);
        }
        if (bytes.Count > 0)
        {
            var request = _engineIOAdapter.ToHttpRequest(bytes);
            await _httpAdapter.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var uri = _uriConverter.GetServerUri(
            false,
            _options.ServerUri,
            _options.Path,
            _options.Query);
        _httpAdapter.Uri = uri;
        var req = new HttpRequest
        {
            Uri = uri,
        };
        try
        {
            await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
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

    public void Dispose()
    {
        if (_engineIOAdapter is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}