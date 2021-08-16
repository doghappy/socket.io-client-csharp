using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.WebSocketClient
{
    public interface IWebSocketClient : IDisposable
    {
        TimeSpan ConnectionTimeout { get; set; }
        Task ConnectAsync(Uri uri);
        Task SendMessageAsync(string text);
        Task SendMessageAsync(string text, CancellationToken cancellationToken);
        Task SendMessageAsync(byte[] bytes);
        Task SendMessageAsync(byte[] bytes, CancellationToken cancellationToken);
        Task DisconnectAsync();
        Action<string> OnTextReceived { get; set; }
        Action<byte[]> OnBinaryReceived { get; set; }
        Action<string> OnClosed { get; set; }
        Action<string> OnError { get; set; }
    }
}
