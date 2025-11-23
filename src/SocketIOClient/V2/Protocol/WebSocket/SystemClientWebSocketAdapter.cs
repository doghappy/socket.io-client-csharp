using System;
using System.Threading;
using System.Threading.Tasks;
using SysWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class SystemClientWebSocketAdapter(IWebSocketClient ws) : IWebSocketClientAdapter
{
    public int SendChunkSize { get; set; } = 1024 * 8;

    public async Task SendAsync(byte[] data, WebSocketMessageType messageType, CancellationToken cancellationToken)
    {
        var type = (SysWebSocketMessageType)messageType;
        var offset = 0;
        bool endOfMessage;
        do
        {
            var length = SendChunkSize;
            endOfMessage = offset + SendChunkSize >= data.Length;
            if (endOfMessage)
            {
                length = data.Length - offset;
            }
            var segment = new ArraySegment<byte>(data, offset, length);
            offset += length;
            await ws.SendAsync(segment, type, endOfMessage, cancellationToken);
        } while (!endOfMessage);
    }
}