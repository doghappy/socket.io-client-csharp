using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public class EventRunner : IEventRunner
{
    public void RunInBackground<T>(EventHandler<T>? handler, object sender, T args)
    {
        if (handler == null)
        {
            return;
        }
        _ = Task.Run(() => handler(sender, args), CancellationToken.None).ConfigureAwait(false);
    }
}