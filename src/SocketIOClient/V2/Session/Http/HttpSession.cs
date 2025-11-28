using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.Http.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session.Http;

public class HttpSession : SessionBase
{
    public HttpSession(
        ILogger<HttpSession> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IHttpAdapter httpAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory,
        IUriConverter uriConverter) : base(logger, uriConverter, httpAdapter)
    {
        _logger = logger;
        _engineIOAdapterFactory = engineIOAdapterFactory;
        _httpAdapter = httpAdapter;
        _serializer = serializer;
        _engineIOMessageAdapterFactory = engineIOMessageAdapterFactory;
        _httpAdapter.Subscribe(this);
    }

    private readonly ILogger<HttpSession> _logger;
    private IEngineIOAdapter _engineIOAdapter;
    private readonly IEngineIOAdapterFactory _engineIOAdapterFactory;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;
    private readonly IEngineIOMessageAdapterFactory _engineIOMessageAdapterFactory;

    protected override Core.Protocol Protocol => Core.Protocol.Polling;

    protected override void OnOptionsChanged(SessionOptions newValue)
    {
        _engineIOAdapter = _engineIOAdapterFactory.Create(newValue.EngineIO);
        _engineIOAdapter.Timeout = newValue.Timeout;
        _engineIOAdapter.Subscribe(this);
        var engineIOMessageAdapter = _engineIOMessageAdapterFactory.Create(newValue.EngineIO);
        _serializer.SetEngineIOMessageAdapter(engineIOMessageAdapter);
    }

    public override async Task OnNextAsync(ProtocolMessage protocolMessage)
    {
        if (protocolMessage.Type == ProtocolMessageType.Bytes)
        {
            var bytesMessages = _engineIOAdapter.ExtractMessagesFromBytes(protocolMessage.Bytes);
            await HandleMessages(bytesMessages).ConfigureAwait(false);
            return;
        }

        var messages = _engineIOAdapter.ExtractMessagesFromText(protocolMessage.Text);
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
        _logger.LogDebug("[Polling⬇] {Text}", text);
        var message = _serializer.Deserialize(text);
        if (message is null)
        {
            return;
        }

        if (message.Type is MessageType.Binary or MessageType.BinaryAck)
        {
            MessageQueue.Enqueue((IBinaryMessage)message);
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
        var message = MessageQueue.Peek();
        message.Add(bytes);
        if (message.ReadyDelivery)
        {
            MessageQueue.Dequeue();
            await OnNextAsync(message).ConfigureAwait(false);
        }
    }

    public override async Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    public override async Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data, packetId);
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

    public override async Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.SerializeAckData(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken)
    {
        _httpAdapter.Uri = uri;
        var req = new HttpRequest
        {
            Uri = uri,
        };
        await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var req = _engineIOAdapter.ToHttpRequest(content);
        await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
    }
}