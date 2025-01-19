using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2;

public interface IProtocolAdapter:IMessageObservable
{
    Task SendAsync(IMessage message);
    Task ConnectAsync(CancellationToken cancellationToken);
}