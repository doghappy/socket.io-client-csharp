using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2;

public interface IProtocolAdapter
{
    Task SendAsync(IProtocolMessage protocolMessage);
    Task ConnectAsync(CancellationToken cancellationToken);
}