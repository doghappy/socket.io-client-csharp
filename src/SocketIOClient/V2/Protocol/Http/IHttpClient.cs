using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public interface IHttpClient
{
    TimeSpan Timeout { get; set; }
    // Task<IHttpResponse> SendAsync(IHttpRequest req);
    Task<IHttpResponse> SendAsync(IHttpRequest req, CancellationToken cancellationToken);
}