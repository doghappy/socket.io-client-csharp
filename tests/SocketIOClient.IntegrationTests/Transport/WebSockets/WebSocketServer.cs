using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.IntegrationTests.Transport.WebSockets
{
    public class WebSocketServer : IDisposable
    {
        public WebSocketServer()
        {
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
            _connections = new List<WebSocket>();
        }

        const int ReceiveChunkSize = ChunkSize.Size8K;

        readonly CancellationTokenSource _cts;
        HttpListener _listener;
        CancellationToken _token;
        bool _disposed;
        List<WebSocket> _connections;

        public Uri ServerUrl { get; private set; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cts.Cancel();
                _cts.Dispose();
                _disposed = true;
            }
        }

        public void AbortAll()
        {
            foreach (var conn in _connections)
            {
                conn.Abort();
            }
        }

        public void Start()
        {
            Console.WriteLine("Requesting a port for WebSocket Server...");
            // IANA suggests the range 49152 to 65535 for dynamic or private ports.
            for (int i = 49152; i < 65536; i++)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{i}/");
                try
                {
                    _listener.Start();
                    ServerUrl = new Uri($"ws://localhost:{i}/");
                    return;
                }
                catch
                {
                    Console.WriteLine($"Port {i} ");
                    continue;
                }
            }
            throw new InvalidProgramException("Unable to start server, ports 49152-65535 are used.");
        }

        public async Task ListenAsync()
        {
            while (!_token.IsCancellationRequested)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                    var webSocket = webSocketContext.WebSocket;
                    _connections.Add(webSocket);
                    while (webSocket.State == System.Net.WebSockets.WebSocketState.Open && !_token.IsCancellationRequested)
                    {
                        await ReadRequestMessage(webSocket);
                    }
                }
            }

            async Task ReadRequestMessage(WebSocket ws)
            {
                var binary = new byte[ReceiveChunkSize];
                int count = 0;
                while (ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var buffer = new byte[ReceiveChunkSize];
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);

                    await SendAsync(ws, buffer, result);

                    // resize
                    if (binary.Length - count < result.Count)
                    {
                        Array.Resize(ref binary, binary.Length + result.Count);
                    }
                    Buffer.BlockCopy(buffer, 0, binary, count, result.Count);
                    count += result.Count;
                    if (result.EndOfMessage)
                    {
                        switch (result.MessageType)
                        {
                            case System.Net.WebSockets.WebSocketMessageType.Text:
                                string text = Encoding.UTF8.GetString(binary, 0, count);
                                OnTextReceived(text);
                                break;
                            case System.Net.WebSockets.WebSocketMessageType.Binary:
                                var bytes = new byte[count];
                                Buffer.BlockCopy(binary, 0, bytes, 0, bytes.Length);
                                OnBinaryReceived(bytes);
                                break;
                            case System.Net.WebSockets.WebSocketMessageType.Close:
                                OnError(new TransportException("Received a Close message"));
                                break;
                        }
                        break;
                    }
                }
            }

            async Task SendAsync(WebSocket ws, byte[] buffer, System.Net.WebSockets.WebSocketReceiveResult result)
            {
                byte[] bytes = new byte[result.Count];
                Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);
                await ws.SendAsync(new ArraySegment<byte>(bytes), result.MessageType, result.EndOfMessage, CancellationToken.None);
            }

            void OnError(TransportException exception)
            {
                Console.WriteLine($"[Server Exception] {exception}");
            }

            void OnTextReceived(string message)
            {
                Console.WriteLine($"[Server Receive] {message}");
            }

            void OnBinaryReceived(byte[] binary)
            {
                string message = Convert.ToBase64String(binary);
                Console.WriteLine($"[Server Receive] {message}");
            }
        }
    }
}