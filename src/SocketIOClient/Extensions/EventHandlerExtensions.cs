using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Extensions
{
    internal static class EventHandlerExtensions
    {
        public static void RunInBackground<T>(this EventHandler<T> handler, object sender, T args)
        {
            _ = Task.Run(() => handler(sender, args), CancellationToken.None).ConfigureAwait(false);
        }
    }
}