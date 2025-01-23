using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol;

public interface IProtocolAdapter : IProtocolMessageObservable
{
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
    Task ConnectAsync(CancellationToken cancellationToken);
}