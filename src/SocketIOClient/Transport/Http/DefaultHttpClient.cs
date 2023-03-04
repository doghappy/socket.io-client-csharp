using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SocketIOClient.Transport.Http
{
    public class DefaultHttpClient : IHttpClient
    {
        public DefaultHttpClient()
        {
            _handler = new HttpClientHandler();
            _httpClient = new HttpClient(_handler);
        }

        readonly HttpClientHandler _handler;
        private readonly HttpClient _httpClient;

        private static readonly HashSet<string> allowedHeaders = new HashSet<string>
        {
            "user-agent",
        };

        public void AddHeader(string name, string value)
        {
            if (_httpClient.DefaultRequestHeaders.Contains(name))
            {
                _httpClient.DefaultRequestHeaders.Remove(name);
            }
            if (allowedHeaders.Contains(name.ToLower()))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Add(name, value);
            }
        }

        public IEnumerable<string> GetHeaderValues(string name)
        {
            return _httpClient.DefaultRequestHeaders.GetValues(name);
        }

        public void SetProxy(IWebProxy proxy)
        {
            _handler.Proxy = proxy;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(request, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            return _httpClient.PostAsync(requestUri, content, cancellationToken);
        }

        public Task<string> GetStringAsync(Uri requestUri)
        {
            return _httpClient.GetStringAsync(requestUri);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _handler.Dispose();
        }
    }
}