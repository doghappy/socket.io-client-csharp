using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public interface IHttpClient
{
    TimeSpan Timeout { get; set; }
    void SetDefaultHeader(string name, string value);
    Task<IHttpResponse> SendAsync(HttpRequest req, CancellationToken cancellationToken);
}