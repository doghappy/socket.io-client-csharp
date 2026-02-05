using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public class TaskDelay : IDelay
{
    public async Task DelayAsync(int ms, CancellationToken cancellationToken)
    {
        await Task.Delay(ms, cancellationToken).ConfigureAwait(false);
    }
}