using System;

namespace SocketIOClient.Exceptions
{
    public sealed class ConnectException : Exception
    {
        public ConnectException(string message) : base(message) { }
        public ConnectException(string message, Exception innerException) : base(message, innerException) { }
        public bool IsTimeout { get; internal set; }
    }
}
