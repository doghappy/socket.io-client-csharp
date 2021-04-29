using System.Text.Json;
using SocketIOClient.Response;

namespace SocketIOClient.Packgers
{
    public class OpenedPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            if (text.StartsWith("{\"sid\":\""))
            {
                var openResponse = new OpenResponse();
                var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                openResponse.Sid = root.GetProperty("sid").GetString();
                openResponse.PingInterval = root.GetProperty("pingInterval").GetInt32();
                openResponse.PingTimeout = root.GetProperty("pingTimeout").GetInt32();
                client.Open(openResponse);
            }
        }
    }
}
