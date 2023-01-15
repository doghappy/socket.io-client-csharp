using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Messages;

namespace SocketIOClient.Transport
{
    public interface ITransport : IDisposable
    {
        Action<IMessage> OnReceived { get; set; }
        Action<Exception> OnError { get; set; }
        string Namespace { get; set; }
        Task SendAsync(IMessage msg, CancellationToken cancellationToken);
        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        void AddHeader(string key, string val);
        void SetProxy(IWebProxy proxy);
    }
}