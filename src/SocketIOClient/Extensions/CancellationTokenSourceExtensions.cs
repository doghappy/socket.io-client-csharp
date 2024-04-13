using System.Threading;

namespace SocketIOClient.Extensions
{
    internal static class CancellationTokenSourceExtensions
    {
        public static void TryDispose(this CancellationTokenSource cts)
        {
            cts?.Dispose();
        }

        public static void TryCancel(this CancellationTokenSource cts)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }

        public static CancellationTokenSourceWrapper Renew(this CancellationTokenSourceWrapper cts)
        {
            if (cts != null)
            {
                cts.Dispose();
            }

            return new CancellationTokenSourceWrapper(new CancellationTokenSource());
        }
    }
}
