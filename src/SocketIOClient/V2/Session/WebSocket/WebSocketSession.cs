using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session.WebSocket;

public class WebSocketSession(
    ILogger<WebSocketSession> logger,
    IWebSocketAdapter wsAdapter,
    IUriConverter uriConverter) : SessionBase(logger, uriConverter, wsAdapter)
{
    protected override Core.Protocol Protocol => Core.Protocol.WebSocket;

    public override Task OnNextAsync(ProtocolMessage message)
    {
        throw new NotImplementedException();
    }

    protected override void OnOptionsChanged(SessionOptions newValue)
    {
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