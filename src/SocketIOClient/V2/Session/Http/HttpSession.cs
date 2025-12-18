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
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.Session.Http.HttpEngineIOAdapter;
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
        IUriConverter uriConverter) : base(logger, engineIOAdapterFactory, httpAdapter, serializer,
        engineIOMessageAdapterFactory, uriConverter)
    {
        _logger = logger;
        _httpAdapter = httpAdapter;
        _serializer = serializer;
    }

    private readonly ILogger<HttpSession> _logger;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;
    private IHttpEngineIOAdapter _httpEngineIOAdapter;

    protected override Core.Protocol Protocol => Core.Protocol.Polling;

    protected override void OnEngineIOAdapterInitialized(IEngineIOAdapter engineIOAdapter)
    {
        _httpEngineIOAdapter = (IHttpEngineIOAdapter)engineIOAdapter;
    }

    public override async Task OnNextAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            var bytesMessages = _httpEngineIOAdapter.ExtractMessagesFromBytes(message.Bytes);
            await HandleMessagesAsync(bytesMessages).ConfigureAwait(false);
            return;
        }

        var messages = _httpEngineIOAdapter.ExtractMessagesFromText(message.Text);
        await HandleMessagesAsync(messages).ConfigureAwait(false);
    }

    protected override void OnOpenedMessage(OpenedMessage message)
    {
        base.OnOpenedMessage(message);
        _httpAdapter.Uri = new Uri($"{_httpAdapter.Uri.AbsoluteUri}&sid={message.Sid}");
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
                var request = _httpEngineIOAdapter.ToHttpRequest(message.Text);
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
            var request = _httpEngineIOAdapter.ToHttpRequest(bytes);
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
        var req = _httpEngineIOAdapter.ToHttpRequest(content);
        await _httpAdapter.SendAsync(req, cancellationToken).ConfigureAwait(false);
    }
}