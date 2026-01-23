using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Serializer;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.Http.EngineIOAdapter;

namespace SocketIOClient.Session.Http;

public class HttpSession : SessionBase<IHttpEngineIOAdapter>
{
    public HttpSession(
        ILogger<HttpSession> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IHttpAdapter httpAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory)
        : base(
            logger,
            engineIOAdapterFactory,
            httpAdapter,
            serializer,
            engineIOMessageAdapterFactory)
    {
        _logger = logger;
        _httpAdapter = httpAdapter;
        _serializer = serializer;
    }

    private readonly ILogger<HttpSession> _logger;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;

    protected override TransportProtocol Protocol => TransportProtocol.Polling;

    public override async Task OnNextAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            var bytesMessages = EngineIOAdapter.ExtractMessagesFromBytes(message.Bytes!);
            await HandleMessagesAsync(bytesMessages).ConfigureAwait(false);
            return;
        }

        var messages = EngineIOAdapter.ExtractMessagesFromText(message.Text!);
        await HandleMessagesAsync(messages).ConfigureAwait(false);
    }

    protected override void OnOpenedMessage(OpenedMessage message)
    {
        base.OnOpenedMessage(message);
        _httpAdapter.Uri = new Uri($"{_httpAdapter.Uri!.AbsoluteUri}&sid={message.Sid}");
    }

    private async Task HandleMessagesAsync(IEnumerable<ProtocolMessage> messages)
    {
        foreach (var message in messages)
        {
            await HandleMessageAsync(message).ConfigureAwait(false);
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
                var request = EngineIOAdapter.ToHttpRequest(message.Text!);
                _logger.LogDebug("[Polling⬆] {text}", request.BodyText);
                await _httpAdapter.SendAsync(request, cancellationToken).ConfigureAwait(false);
                continue;
            }
            _logger.LogDebug("[Polling⬆] 0️⃣1️⃣0️⃣1️⃣ {length}", message.Bytes!.Length);
            bytes.Add(message.Bytes);
        }

        if (bytes.Count > 0)
        {
            var request = EngineIOAdapter.ToHttpRequest(bytes);
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

    protected override string GetServerUriSchema()
    {
        var schema = Options.ServerUri.Scheme.ToLowerInvariant();
        return schema switch
        {
            "http" or "ws" => "http",
            "https" or "wss" => "https",
            _ => throw new ArgumentException("Only supports 'http, https, ws, wss' protocol")
        };
    }

    protected override NameValueCollection GetProtocolQueries()
    {
        return new NameValueCollection
        {
            ["transport"] = "polling"
        };
    }

    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var req = EngineIOAdapter.ToHttpRequest(content);
        await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
    }
}