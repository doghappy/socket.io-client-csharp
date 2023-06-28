using System;
using System.Collections.Generic;

namespace SocketIO.Core
{
    public interface IMessage2
    {
        MessageType Type { get; }
        string Sid { get; set; }
        int PingInterval { get; set; }
        int PingTimeout { get; set; }
        List<string> Upgrades { get; set; }
        int BinaryCount { get; set; }
        List<byte[]> OutgoingBytes { get; set; }
        [Obsolete]
        List<byte[]> IncomingBytes { get; set; }
        string Namespace { get; set; }
        TimeSpan Duration { get; set; }
        int Id { get; set; }

        string Event { get; set; }
        string Error { get; set; }
        
        // TODO: move this to sub class
        string ReceivedText { get; set; }
        List<byte[]> ReceivedBinary { get; set; }
    }
}