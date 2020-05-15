using Newtonsoft.Json;
using SocketIOClient.Packgers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Websocket.Client;

namespace SocketIOClient.WebSocketClient
{
    public class WebSocketClient : IWebSocketClient
    {
        public WebSocketClient(SocketIO io, PackgeManager parser)
        {
            _parser = parser;
            _io = io;
        }

        readonly PackgeManager _parser;
        readonly SocketIO _io;
        WebsocketClient _client;

        public async Task ConnectAsync(Uri uri, WebSocketConnectionOptions options)
        {
            _client = new WebsocketClient(uri)
            {
                IsReconnectionEnabled = false,
                ReconnectTimeout = options.ConnectionTimeout
            };

            _client.MessageReceived.Subscribe(message =>
            {
                if (message.MessageType == WebSocketMessageType.Text)
                {
#if DEBUG
                    Trace.WriteLine($"⬇ {DateTime.Now} {message.Text}");
#endif
                    _parser.Unpack(message.Text);
                }
                else if (message.MessageType == WebSocketMessageType.Binary)
                {
#if DEBUG
                    Trace.WriteLine($"⬇ {DateTime.Now} {JsonConvert.SerializeObject(message.Binary)}");
#endif
                    _io.InvokeBytesReceived(message.Binary.Skip(1).ToArray());
                }
            });

            _client.DisconnectionHappened.Subscribe(disconnectionInfo =>
            {
                string reason = null;
                switch (disconnectionInfo.Type)
                {
                    case DisconnectionType.Exit:
                        break;
                    case DisconnectionType.Lost:
                        reason = "transport close";
                        break;
                    case DisconnectionType.NoMessageReceived:
                        break;
                    case DisconnectionType.Error:
                        break;
                    case DisconnectionType.ByUser:
                        break;
                    case DisconnectionType.ByServer:
                        break;
                    default:
                        break;
                }
                if (reason != null)
                {
                    _io.InvokeDisconnect(reason);
                }
            });

            await _client.Start();
        }

        public async Task SendMessageAsync(string text)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Faild to emit, websocket is not connected yet.");
            }
            await _client.SendInstant(text);
#if DEBUG
            Trace.WriteLine($"⬆ {DateTime.Now} {text}");
#endif
        }

        public async Task SendMessageAsync(byte[] bytes)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Faild to emit, websocket is not connected yet.");
            }
            await _client.SendInstant(bytes);
#if DEBUG
            Trace.WriteLine($"⬆ {DateTime.Now} {bytes}");
#endif
        }

        public async Task DisconnectAsync()
        {
            await _client.Stop(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure));
        }
    }
}
