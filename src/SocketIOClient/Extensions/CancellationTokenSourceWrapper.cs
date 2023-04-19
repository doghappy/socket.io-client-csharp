using System;
using System.Threading;

namespace SocketIOClient.Extensions
{
    class CancellationTokenSourceWrapper : IDisposable
    {
        public CancellationTokenSourceWrapper(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        private readonly CancellationTokenSource _cts;

        private bool _cancelled;
        private bool _disposed;

        private void Cancel()
        {
            if (_cancelled)
            {
                return;
            }
            _cts.Cancel();
            _cancelled = true;
        }

        private void DisposeInternal()
        {
            if (_disposed)
            {
                return;
            }
            _cts.Dispose();
            _disposed = true;
        }

        public CancellationToken Token => _cts.Token;

        public bool IsCancellationRequested => _cts.IsCancellationRequested;

        public void Dispose()
        {
            Cancel();
            DisposeInternal();
        }
    }
}