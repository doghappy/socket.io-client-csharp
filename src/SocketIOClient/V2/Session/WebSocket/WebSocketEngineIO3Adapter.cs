using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.WebSocket;

public class WebSocketEngineIO3Adapter : EngineIO3Adapter
{
    public WebSocketEngineIO3Adapter(
        IStopwatch stopwatch,
        ILogger<WebSocketEngineIO3Adapter> logger,
        IPollingHandler pollingHandler,
        IWebSocketAdapter webSocketAdapter) : base(stopwatch, logger, pollingHandler)
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
}