using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session.WebSocket;

public class WebSocketSession(
    ILogger<WebSocketSession> logger,
    IEngineIOAdapterFactory engineIOAdapterFactory,
    IWebSocketAdapter wsAdapter,
    ISerializer serializer,
    IEngineIOMessageAdapterFactory engineIOMessageAdapterFactory,
    IUriConverter uriConverter)
    : SessionBase(logger, engineIOAdapterFactory, wsAdapter, serializer,
        engineIOMessageAdapterFactory, uriConverter)
{
    protected override Core.Protocol Protocol => Core.Protocol.WebSocket;

    protected override void OnEngineIOAdapterInitialized(IEngineIOAdapter engineIOAdapter)
    {
    }

    public override async Task OnNextAsync(ProtocolMessage message)
    {
        await HandleMessageAsync(message).ConfigureAwait(false);
    }

    public override Task SendAsync(object[] data, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override async Task ConnectCoreAsync(Uri uri, CancellationToken cancellationToken)
    {
        await wsAdapter.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public override async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(Options.Namespace) ? "41" : $"41{Options.Namespace},";
        var message = new ProtocolMessage { Text = content };
        await wsAdapter.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }
}