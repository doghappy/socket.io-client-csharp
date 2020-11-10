using System;
using System.Threading.Tasks;

namespace SocketIOClient.WebSocketClient
{
    public interface IWebSocketClient
    {
        Task ConnectAsync(Uri uri);
        Task SendMessageAsync(string text);
        Task SendMessageAsync(byte[] bytes);
        Task DisconnectAsync();
    }
}
