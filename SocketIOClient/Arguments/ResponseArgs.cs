using System.Collections.Generic;

namespace SocketIOClient.Arguments
{
    public class ResponseArgs
    {
        public string RawText { get; set; }
        public string Text { get; set; }
        public List<byte[]> Buffers { get; set; }
    }
}
