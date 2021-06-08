using System;

namespace SocketIOClient.Test.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class SocketIOVersionAttribute : Attribute
    {
        public SocketIOVersionAttribute(SocketIOVersion version)
        {
            Version = version;
        }

        public SocketIOVersion Version { get; }
    }

    enum SocketIOVersion
    {
        V2,
        V3,
        V4
    }
}
