using System;
using System.Net;
using System.Net.Http;

namespace SocketIOClient.Transport.Http
{
    public interface IHttpClientAdapter : IDisposable
    {
        HttpClient HttpClient { get; }
        void AddHeader(string name, string value);
        void SetProxy(IWebProxy proxy);
    }
}
