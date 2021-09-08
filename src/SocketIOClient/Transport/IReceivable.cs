using System;

namespace SocketIOClient.Transport
{
    public interface IReceivable
    {
        Action<string> OnTextReceived { get; set; }
        Action<byte[]> OnBinaryReceived { get; set; }
    }
}
