using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public interface IHttpAdapter : IProtocolAdapter
{
    Task SendAsync(IHttpRequest req, CancellationToken cancellationToken);
}