using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Sessions
{
    public class Session
    {
        public Session(Uri serverUri)
        {
            UriConverter = new UriConverter();
            Eio = 4;
            Path = "/socket.io";
            ServerUri = serverUri;
        }

        public IUriConverter UriConverter { get; set; }
        public int Eio { get; set; }
        public Uri ServerUri { get; }
        public string Path { get; set; }
        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; set; }

        // 会话层
        // 负责 ping pong、握手、选择传输实现，
        // 为上层屏蔽 socket.io 版本差异，上层需要告诉会话层，eio 版本，基于选择的传输实现选择消息转换器。
        public int EIO { get; set; }

        public async Task HandshakeAsync(Uri serverUri)
        {
            Uri uri = UriConverter.GetHandshakeUri(ServerUri, Eio, Path, QueryParams);
        }
    }
}
