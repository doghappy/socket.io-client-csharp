using System;
using System.Threading.Tasks;

namespace SocketIOClient.Protocols
{
    interface IProtocol
    {
        Action<string> OnTextReceived { get; set; }
        Action<byte[]> OnBinaryReceived { get; set; }

        Task SendAsync(byte[] data);
        Task SendAsync(string data);
    }
}
