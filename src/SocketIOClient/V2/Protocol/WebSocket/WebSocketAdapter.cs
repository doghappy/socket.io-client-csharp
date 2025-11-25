using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class WebSocketAdapter(ILogger<WebSocketAdapter> logger, IWebSocketClientAdapter clientAdapter)
    : IWebSocketAdapter
{
    public void Subscribe(IMyObserver<ProtocolMessage> observer)
    {
        throw new NotImplementedException();
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        var isTextButNull = message.Type == ProtocolMessageType.Text && message.Text == null;
        var isBytesButNull = message.Type == ProtocolMessageType.Bytes && message.Bytes == null;
        if (isTextButNull || isBytesButNull)
        {
            throw new ArgumentNullException();
        }

        if (message.Type == ProtocolMessageType.Text)
        {
            var data = Encoding.UTF8.GetBytes(message.Text);
            await clientAdapter.SendAsync(data, WebSocketMessageType.Text, cancellationToken).ConfigureAwait(false);
            return;
        }

        await clientAdapter
            .SendAsync(message.Bytes, WebSocketMessageType.Binary, cancellationToken)
            .ConfigureAwait(false);
    }
}