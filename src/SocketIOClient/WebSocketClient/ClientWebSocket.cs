using SocketIOClient.Packgers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace SocketIOClient.WebSocketClient
{
    public class ClientWebSocket : IWebSocketClient
    {
        public ClientWebSocket(SocketIO io, PackgeManager parser)
        {
            _parser = parser;
            _io = io;
        }

        const int ReceiveChunkSize = 1024;
        const int SendChunkSize = 1024;

        readonly PackgeManager _parser;
        readonly SocketIO _io;
        System.Net.WebSockets.Managed.ClientWebSocket _ws;
        CancellationTokenSource _connectionToken;

        public async Task ConnectAsync(Uri uri, WebSocketConnectionOptions options)
        {
            _ws = new System.Net.WebSockets.Managed.ClientWebSocket();
            _connectionToken = new CancellationTokenSource();
            await _ws.ConnectAsync(uri, _connectionToken.Token);
            await Task.Factory.StartNew(ListenAsync, _connectionToken.Token);
        }

        public async Task SendMessageAsync(string text)
        {
            if (_ws == null)
            {
                throw new InvalidOperationException("Faild to emit, websocket is not connected yet.");
            }
            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }

            var messageBuffer = Encoding.UTF8.GetBytes(text);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > messageBuffer.Length)
                {
                    count = messageBuffer.Length - offset;
                }

                await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _connectionToken.Token);
            }
#if DEBUG
            Trace.WriteLine($"⬆ {DateTime.Now} {text}");
#endif
        }

        public async Task SendMessageAsync(byte[] bytes)
        {
            if (_ws == null)
            {
                throw new InvalidOperationException("Faild to emit, websocket is not connected yet.");
            }
            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }
            var messagesCount = (int)Math.Ceiling((double)bytes.Length / SendChunkSize);
            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > bytes.Length)
                {
                    count = bytes.Length - offset;
                }

                await _ws.SendAsync(new ArraySegment<byte>(bytes, offset, count), WebSocketMessageType.Binary, lastMessage, _connectionToken.Token);
            }
#if DEBUG
            Trace.WriteLine($"⬆ {DateTime.Now} {bytes}");
#endif
        }

        public async Task DisconnectAsync()
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            _connectionToken.Cancel();
        }

        private async Task ListenAsync()
        {
            var buffer = new byte[ReceiveChunkSize];
            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    var binaryResult = new List<byte>();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _connectionToken.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            _connectionToken.Cancel();
                            _io.InvokeDisconnect("io server disconnect");
                        }
                        else if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            binaryResult.AddRange(buffer.Take(result.Count));
                        }
                    } while (!result.EndOfMessage);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = stringResult.ToString();
#if DEBUG
                        Trace.WriteLine($"⬇ {DateTime.Now} {message}");
#endif
                        _parser.Unpack(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
#if DEBUG
                        var builder = new StringBuilder();
                        binaryResult.ForEach(b => builder.Append(b));
                        Trace.WriteLine($"⬇ {DateTime.Now} {builder}");
#endif
                        _io.InvokeBytesReceived(binaryResult.Skip(1).ToArray());
                    }
                }
            }
            //catch (Exception e)
            //{
            //    CallOnDisconnected();
            //}
            finally
            {
                _ws.Dispose();
            }
        }
    }
}
