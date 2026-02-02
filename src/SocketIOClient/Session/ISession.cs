using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Observers;

namespace SocketIOClient.Session;

public interface ISession : IMyObserver<ProtocolMessage>, IMyObservable<IMessage>, IMyObserver<IMessage>
{
    int PendingDeliveryCount { get; }
    SessionOptions Options { get; set; }
    Task SendAsync(object[] data, CancellationToken cancellationToken);
    Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);
    Task SendAckDataAsync(object[] data, int packetId, CancellationToken cancellationToken);

    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}