using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public interface IHttpAdapter : IProtocolAdapter // TODO: seems no need IProtocolAdapter.SendAsync(pm)
{
    Task SendAsync(IHttpRequest req, CancellationToken cancellationToken);
    Uri Uri { get; set; }
    bool IsReadyToSend { get; }
}