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

    public byte[] WriteProtocolFrame(byte[] bytes)
    {
        byte[] buffer = new byte[bytes.Length + 1];
        buffer[0] = 4;
        Buffer.BlockCopy(bytes, 0, buffer, 1, bytes.Length);
        return buffer;
    }

    public byte[] ReadProtocolFrame(byte[] bytes)
    {
        var result = new byte[bytes.Length - 1];
        Buffer.BlockCopy(bytes, 1, result, 0, result.Length);
        return result;
    }
}