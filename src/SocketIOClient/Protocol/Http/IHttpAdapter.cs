using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Protocol.Http;

public interface IHttpAdapter : IProtocolAdapter
{
    Task SendAsync(HttpRequest req, CancellationToken cancellationToken);
    Uri Uri { get; set; }
    bool IsReadyToSend { get; }
}