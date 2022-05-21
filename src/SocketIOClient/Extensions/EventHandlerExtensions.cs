using System;

namespace SocketIOClient.Extensions
{
    internal static class EventHandlerExtensions
    {
        public static void TryInvoke<T>(this EventHandler<T> handler, object sender, T args)
        {
            handler?.Invoke(sender, args);
        }
    }
}
