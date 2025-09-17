using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2;

public interface IInternalSocketIO : IMyObserver<IMessage>
{
    Task SendAckDataAsync(int packetId, IEnumerable<object> data);
    Task SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken);
}