using System.Net;
using System.Net.Http;

namespace SocketIOClient.Transport.Http
{
    public class DefaultHttpClientAdapter : IHttpClientAdapter
    {
        public DefaultHttpClientAdapter()
        {
            _handler = new HttpClientHandler();
            HttpClient = new HttpClient(_handler);
        }

        readonly HttpClientHandler _handler;

        public HttpClient HttpClient { get; }

        public void AddHeader(string name, string value)
        {
            HttpClient.DefaultRequestHeaders.Add(name, value);
        }

        public void SetProxy(IWebProxy proxy)
        {
            _handler.Proxy = proxy;
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            _handler.Dispose();
        }
    }
}
