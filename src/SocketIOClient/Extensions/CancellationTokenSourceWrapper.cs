using System;
using System.Threading;

namespace SocketIOClient.Extensions
{
    class CancellationTokenSourceWrapper(CancellationTokenSource cts) : IDisposable
    {
        private bool _cancelled;
        private bool _disposed;

        private void Cancel()
        {
            if (_cancelled)
            {
                return;
            }
            cts.Cancel();
            _cancelled = true;
        }

        private void DisposeInternal()
        {
            if (_disposed)
            {
                return;
            }
            cts.Dispose();
            _disposed = true;
        }

        public CancellationToken Token => cts.Token;

        public bool IsCancellationRequested => cts.IsCancellationRequested;

        public void Dispose()
        {
            Cancel();
            DisposeInternal();
        }
    }
}