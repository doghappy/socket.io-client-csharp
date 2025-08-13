using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2;

public interface IInternalSocketIO
{
    Task SendAckDataAsync(int packetId, IEnumerable<object> data);
    Task SendAckDataAsync(int packetId, IEnumerable<object> data, CancellationToken cancellationToken);
}