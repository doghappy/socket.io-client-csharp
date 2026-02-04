using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public interface IDelay
{
    Task DelayAsync(int ms, CancellationToken cancellationToken);
}