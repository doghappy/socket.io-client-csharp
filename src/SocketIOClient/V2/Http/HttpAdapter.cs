using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Http;

public class HttpAdapter : IHttpAdapter
{
    public HttpAdapter(IHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    IHttpClient _httpClient;

    public Task SendAsync(IProtocolMessage protocolMessage)
    {
        throw new System.NotImplementedException();
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public void OnNext(IHttpResponse response)
    {
        throw new NotImplementedException();
    }
}