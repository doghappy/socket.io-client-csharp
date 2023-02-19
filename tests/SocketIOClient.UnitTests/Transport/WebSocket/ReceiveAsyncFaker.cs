using System;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTests.Transport.WebSocket;

class ReceiveAsyncFaker
{
    private int offset;

    public async Task<WebSocketReceiveResult> ReceiveAsync(TransportMessageType type, byte[] data, Func<Task> done)
    {
        if (offset >= data.Length)
        {
            await done();
        }
        var buffer = new byte[ChunkSize.Size8K];
        int count = data.Length - offset;
        if (count > buffer.Length)
        {
            count = buffer.Length;
        }
        Buffer.BlockCopy(data, offset, buffer, 0, count);
        offset += count;
        bool endOfMessage = false;
        if (offset >= data.Length)
        {
            endOfMessage = true;
            Reset();
        }
        return new WebSocketReceiveResult
        {
            MessageType = type,
            EndOfMessage = endOfMessage,
            Buffer = buffer,
            Count = count,
        };
    }

    private void Reset()
    {
        offset = 0;
    }
}