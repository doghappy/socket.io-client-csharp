using System;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTest.Transport.WebSocket;

class ReceiveAsyncFaker
{
    public ReceiveAsyncFaker(EngineIO eio)
    {
        this.eio = eio;
    }

    private readonly EngineIO eio;
    private int offset;

    public async Task<WebSocketReceiveResult> ReceiveAsync(TransportMessageType type, byte[] data, Func<Task> done)
    {
        /* if (offset >= data.Length)
        {
            await done();
        }
        var buffer = new byte[ChunkSize.Size8K];
        int dstOffset = 0;
        int count = data.Length - offset;
        if (count > buffer.Length)
        {
            count = buffer.Length;
        }
        if (eio == EngineIO.V3 && type == TransportMessageType.Binary && offset == 0)
        {
            dstOffset = 1;
        }
        int actualCount = count - dstOffset;
        Buffer.BlockCopy(data, offset, buffer, dstOffset, actualCount);
        offset += actualCount;
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
        };*/
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