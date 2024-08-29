using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport.Http
{
    public interface IHttpClient : IDisposable
    {
        void AddHeader(string name, string value);
        void SetProxy(IWebProxy proxy);
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken);
        Task<string> GetStringAsync(Uri requestUri);
    }
}