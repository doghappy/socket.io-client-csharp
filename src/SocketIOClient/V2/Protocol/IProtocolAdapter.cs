using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Protocol;

public interface IProtocolAdapter : IMyObservable<ProtocolMessage>
{
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
    Task ConnectAsync(CancellationToken cancellationToken);
}