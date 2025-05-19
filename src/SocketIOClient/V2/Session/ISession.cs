using System;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Session;

public interface ISession : IMyObserver<ProtocolMessage>, IMyObservable<IMessage>, IMyObserver<IMessage>, IDisposable
{
    int PendingDeliveryCount { get; }
    Task SendAsync(object[] data, CancellationToken cancellationToken);
    Task SendAsync(object[] data, int packetId, CancellationToken cancellationToken);

    Task ConnectAsync(CancellationToken cancellationToken);
    // Task DisconnectAsync(CancellationToken cancellationToken);
}