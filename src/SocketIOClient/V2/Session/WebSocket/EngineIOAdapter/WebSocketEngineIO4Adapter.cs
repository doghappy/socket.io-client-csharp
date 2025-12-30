using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;

public class WebSocketEngineIO4Adapter : EngineIO4Adapter, IWebSocketEngineIOAdapter
{
    public WebSocketEngineIO4Adapter(
        IStopwatch stopwatch,
        ISerializer serializer,
        IWebSocketAdapter webSocketAdapter) : base(stopwatch, serializer)
    {
        _webSocketAdapter = webSocketAdapter;
    }

    private readonly IWebSocketAdapter _webSocketAdapter;

    protected override async Task SendConnectAsync(string message)
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = message,
        }, cts.Token).ConfigureAwait(false);
    }

    protected override async Task SendPongAsync()
    {
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _webSocketAdapter.SendAsync(new ProtocolMessage
        {
            Text = "3"
        }, cts.Token).ConfigureAwait(false);
    }

    public byte[] WriteProtocolFrame(byte[] bytes)
    {
        return bytes;
    }

    public byte[] ReadProtocolFrame(byte[] bytes)
    {
        return bytes;
    }
}