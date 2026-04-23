using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, IErrorStrategy errorStrategy)
    {
        _ = task
            .ContinueWith(async t => await errorStrategy.OnErrorAsync(t.Exception!), TaskContinuationOptions.OnlyOnFaulted)
            .ConfigureAwait(false);
    }
}