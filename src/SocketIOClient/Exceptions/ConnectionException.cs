using System;

namespace SocketIOClient
{
    public class ConnectionException : Exception
    {
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
