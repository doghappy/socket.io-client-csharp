using System;

namespace SocketIOClient.Exceptions
{
    public sealed class InvalidSocketStateException : Exception
    {
        public InvalidSocketStateException() : this("The socket state is not 'Open', and no messages can be sent.") { }
        public InvalidSocketStateException(string message) : base(message) { }
    }
}
