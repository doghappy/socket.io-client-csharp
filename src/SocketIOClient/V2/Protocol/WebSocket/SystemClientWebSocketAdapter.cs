using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SysWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class SystemClientWebSocketAdapter(IWebSocketClient ws) : IWebSocketClientAdapter
{
    public int SendChunkSize { get; set; } = 1024 * 8;
    public int ReceiveChunkSize { get; set; } = 1024 * 8;

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
            await ws.SendAsync(segment, type, endOfMessage, cancellationToken).ConfigureAwait(false);
        } while (!endOfMessage);
    }

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        return ws.ConnectAsync(uri, cancellationToken);
    }

    public async Task<WebSocketMessage> ReceiveAsync(CancellationToken cancellationToken)
    {
        var bytes = new byte[ReceiveChunkSize];
        var buffer = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(bytes), cancellationToken).ConfigureAwait(false);
            await buffer.WriteAsync(bytes, 0, result.Count, cancellationToken);
        } while (!result.EndOfMessage);

        return new WebSocketMessage
        {
            Bytes = buffer.ToArray(),
            Type = (WebSocketMessageType)result.MessageType,
        };
    }
}