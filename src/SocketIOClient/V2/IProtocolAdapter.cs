using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2;

public interface IProtocolAdapter : IProtocolMessageObservable
{
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
    Task ConnectAsync(CancellationToken cancellationToken);
}