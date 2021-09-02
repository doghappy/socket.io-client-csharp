using System.Net.WebSockets;

namespace SocketIOClient.WebSocketClient
{
    public class Message
    {
        public WebSocketMessageType Type { get; set; }
        public string Text { get; set; }
        public byte[] Binary { get; set; }

        public override string ToString()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
