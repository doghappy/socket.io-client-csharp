using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.WebSocketClient
{
    public interface IWebSocketClient
    {
        Task ConnectAsync(Uri uri);
        Task SendMessageAsync(string text);
        Task SendMessageAsync(string text, CancellationToken cancellationToken);
        Task SendMessageAsync(byte[] bytes);
        Task SendMessageAsync(byte[] bytes, CancellationToken cancellationToken);
        Task DisconnectAsync();
    }
}
