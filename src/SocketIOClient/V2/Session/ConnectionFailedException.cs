using System;

namespace SocketIOClient.V2.Session;

public class ConnectionFailedException(string message, Exception innerException) : Exception(message, innerException)
{
    public ConnectionFailedException(Exception innerException) : this("Failed to connect to the server", innerException)
    {
    }
}