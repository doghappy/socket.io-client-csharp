using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;

namespace SocketIOClient.V2.Session.WebSocket;

public class WebSocketSession : SessionBase<IWebSocketEngineIOAdapter>
{
    public WebSocketSession(
        ILogger<WebSocketSession> logger,
        IEngineIOAdapterFactory engineIOAdapterFactory,
        IWebSocketAdapter wsAdapter,
        ISerializer serializer,
        IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory)
        : base(
            logger,
            engineIOAdapterFactory,
            wsAdapter,
            serializer,
            engineIOMessageAdapterFactory)
    {
        _logger = logger;
        _serializer = serializer;
        _wsAdapter = wsAdapter;
    }

    private readonly ISerializer _serializer;
    private readonly IWebSocketAdapter _wsAdapter;
    private readonly ILogger<WebSocketSession> _logger;

    protected override TransportProtocol Protocol => TransportProtocol.WebSocket;

    public override async Task OnNextAsync(ProtocolMessage message)
    {
        if (message.Type == ProtocolMessageType.Bytes)
        {
            message.Bytes = EngineIOAdapter.ReadProtocolFrame(message.Bytes);
        }
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
            if (message.Type == ProtocolMessageType.Text)
            {
                _logger.LogDebug("[WebSocket⬆] {message}", message.Text);
            }
            else
            {
                message.Bytes = EngineIOAdapter.WriteProtocolFrame(message.Bytes);
                _logger.LogDebug("[WebSocket⬆] 0️⃣1️⃣0️⃣1️⃣ {length}", message.Bytes.Length);
            }

            await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.Serialize(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    public override async Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        var messages = _serializer.SerializeAckData(data, packetId);
        await SendProtocolMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _wsAdapter.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    protected override string GetServerUriSchema()
    {
        var schema = Options.ServerUri.Scheme.ToLowerInvariant();
        return schema switch
        {
            "http" or "ws" => "ws",
            "https" or "wss" => "wss",
            _ => throw new ArgumentException("Only supports 'http, https, ws, wss' protocol")
        };
    }

    protected override void SetProtocolQueries(NameValueCollection query)
    {
        query["transport"] = "websocket";
        if (!string.IsNullOrEmpty(Options.Sid))
        {
            query["sid"] = Options.Sid;
        }
    }

    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var message = new ProtocolMessage { Text = content };
        await _wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }
}