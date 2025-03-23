using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Session;

public interface ISession : IMyObserver<ProtocolMessage>, IMyObservable<IMessage>
{
    int PendingDeliveryCount { get; }
    SessionOptions SessionOptions { get; set; }
    Task SendAsync(IMessage message, CancellationToken cancellationToken);
    Task ConnectAsync(CancellationToken cancellationToken);
}