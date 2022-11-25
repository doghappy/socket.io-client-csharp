using System;

namespace SocketIOClient.Transport
{
    public class ObjectNotCleanException : Exception
    {
        public ObjectNotCleanException() : base("Object is not clean, may need to create a new object.") { }
    }
}
