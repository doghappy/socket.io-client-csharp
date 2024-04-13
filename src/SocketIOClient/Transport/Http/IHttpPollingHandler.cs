using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport.Http
{
    public interface IHttpPollingHandler
    {
        Func<string, Task> OnTextReceived { get; set; }
        Func<byte[], Task> OnBytesReceived { get; set; }
        Task GetAsync(string uri, CancellationToken cancellationToken);
        Task SendAsync(HttpRequestMessage req, CancellationToken cancellationToken);
        Task PostAsync(string uri, string content, CancellationToken cancellationToken);
        Task PostAsync(string uri, IEnumerable<byte[]> bytes, CancellationToken cancellationToken);
        void AddHeader(string key, string val);
        void SetProxy(IWebProxy proxy);
    }
}