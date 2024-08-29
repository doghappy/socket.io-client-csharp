using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIOClient.Transport
{
    public interface ITransport : IDisposable
    {
        Action<IMessage> OnReceived { get; set; }
        Action<Exception> OnError { get; set; }
        Task SendAsync(IList<SerializedItem> items, CancellationToken cancellationToken);
        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        void AddHeader(string key, string val);
        void SetProxy(IWebProxy proxy);
    }
}