using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Reactive.Subjects;

namespace SocketIOClient.WebSocketClient
{
    public interface IWebSocketClient : IDisposable
    {
        int ReceiveChunkSize { get; set; }

        int SendChunkSize { get; set; }

        TimeSpan ConnectionTimeout { get; set; }

        TimeSpan ReceiveWait { get; set; }

        IObservable<Exception> OnListenError { get; }

        IObservable<Unit> OnAborted { get; }

        /// <exception cref="WebSocketException"></exception>
        Task ConnectAsync(Uri uri);

        Task DisconnectAsync(CancellationToken cancellationToken);

        Task<bool> TryDisconnectAsync(CancellationToken cancellationToken);

        IConnectableObservable<Message> Listen();

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        Task SendAsync(byte[] bytes, CancellationToken cancellationToken);

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="WebSocketException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        Task SendAsync(string text, CancellationToken cancellationToken);

        Task<bool> TrySendAsync(byte[] bytes, CancellationToken cancellationToken);

        Task<bool> TrySendAsync(string text, CancellationToken cancellationToken);
    }
}
