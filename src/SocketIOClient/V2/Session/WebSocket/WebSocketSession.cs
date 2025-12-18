using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session.WebSocket;

public class WebSocketSession : SessionBase
{
    public WebSocketSession(
        ILogger<WebSocketSession> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IWebSocketAdapter wsAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory,
        IUriConverter uriConverter) : base(logger, engineIOAdapterFactory, wsAdapter, serializer,
        engineIOMessageAdapterFactory, uriConverter)
    {
        _serializer = serializer;
        _wsAdapter = wsAdapter;
    }

    private readonly ISerializer _serializer;
    private readonly IWebSocketAdapter _wsAdapter;

    protected override Core.Protocol Protocol => Core.Protocol.WebSocket;

    protected override void OnEngineIOAdapterInitialized(IEngineIOAdapter engineIOAdapter)
    {
    }

    public override async Task OnNextAsync(ProtocolMessage message)
    {
        await HandleMessageAsync(message).ConfigureAwait(false);
    }

    public override async Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendProtocolMessagesAsync(IEnumerable<ProtocolMessage> messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
#if DEBUG
            var text = message.Type == ProtocolMessageType.Text
                ? $"[WebSocket⬆] {message.Text}"
                : $"[WebSocket⬆] 0️⃣1️⃣0️⃣1️⃣ {message.Bytes.Length}";
            Debug.WriteLine(text);
#endif
            await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    public override Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override async Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _wsAdapter.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var message = new ProtocolMessage { Text = content };
        await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }
}