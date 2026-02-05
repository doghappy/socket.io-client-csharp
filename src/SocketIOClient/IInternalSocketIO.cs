using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Common.Messages;
using SocketIOClient.Observers;

namespace SocketIOClient;

public interface IInternalSocketIO : IMyObserver<IMessage>
{
    int AckHandlerCount { get; }
    int PacketId { get; }
    Task SendAckDataAsync(int packetId, IEnumerable<object> data);
    Task SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken);
}