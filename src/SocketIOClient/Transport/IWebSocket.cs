using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public interface IWebSocket : IObservable<TransportMessage>, IObservable<Exception>, IDisposable
    {
        TimeSpan ConnectionTimeout { get; set; }
        Task ConnectAsync(Uri uri);
        Task DisconnectAsync(CancellationToken cancellationToken);
        Task SendAsync(byte[] bytes, CancellationToken cancellationToken);
        Task SendAsync(string text, CancellationToken cancellationToken);
    }
}
