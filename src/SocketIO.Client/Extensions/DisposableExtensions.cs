using System;

namespace SocketIO.Client.Extensions
{
    internal static class DisposableExtensions
    {
        public static void TryDispose(this IDisposable disposable)
        {
            disposable?.Dispose();
        }
    }
}
