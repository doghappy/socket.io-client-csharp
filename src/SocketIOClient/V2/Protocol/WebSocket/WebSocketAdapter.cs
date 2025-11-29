using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class WebSocketAdapter(ILogger<WebSocketAdapter> logger, IWebSocketClientAdapter clientAdapter)
    : ProtocolAdapter, IWebSocketAdapter, IDisposable
{
    private readonly CancellationTokenSource _receiveCancellationTokenSource = new();

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        await clientAdapter.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        var token = _receiveCancellationTokenSource.Token;
        _ = Task.Run(() => ReceiveAsync(token), token);
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await clientAdapter.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            var protocolMessage = new ProtocolMessage();
            switch (message.Type)
            {
                case WebSocketMessageType.Text:
                    protocolMessage.Type = ProtocolMessageType.Text;
                    protocolMessage.Text = Encoding.UTF8.GetString(message.Bytes);
                    break;
                case WebSocketMessageType.Binary:
                    protocolMessage.Type = ProtocolMessageType.Bytes;
                    protocolMessage.Bytes = message.Bytes;
                    break;
                default:
                    throw new NotImplementedException();
            }
            await OnNextAsync(protocolMessage).ConfigureAwait(false);
        }
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

    public override void SetDefaultHeader(string name, string value) => clientAdapter.SetDefaultHeader(name, value);

    public void Dispose()
    {
        _receiveCancellationTokenSource.Cancel();
        _receiveCancellationTokenSource.Dispose();
    }
}