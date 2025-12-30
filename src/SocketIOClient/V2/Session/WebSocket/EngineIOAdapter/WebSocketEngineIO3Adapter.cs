using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;

public class WebSocketEngineIO3Adapter : EngineIO3Adapter, IWebSocketEngineIOAdapter
{
    public WebSocketEngineIO3Adapter(
        IStopwatch stopwatch,
        ILogger<WebSocketEngineIO3Adapter> logger,
        IWebSocketAdapter webSocketAdapter) : base(stopwatch, logger)
    {
        _webSocketAdapter = webSocketAdapter;
    }

    private readonly IWebSocketAdapter _webSocketAdapter;

    protected override async Task SendConnectAsync()
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = $"40{Options.Namespace},"
        }, cts.Token).ConfigureAwait(false);
    }

    protected override async Task SendPingAsync()
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = "2"
        }, cts.Token).ConfigureAwait(false);
    }

    public void FormatBytesMessage(ProtocolMessage message)
    {
        byte[] buffer = new byte[message.Bytes.Length + 1];
        buffer[0] = 4;
        Buffer.BlockCopy(message.Bytes, 0, buffer, 1, message.Bytes.Length);
        message.Bytes = buffer;
    }
}