using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Packgers
{
    public class MessageAckPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            if (!string.IsNullOrEmpty(client.Namespace) && text.StartsWith(client.Namespace))
            {
                text = text.Substring(client.Namespace.Length);
            }
            int index = text.IndexOf('[');
            if (index > 0)
            {
                string no = text.Substring(0, index);
                string data = text.Substring(index);
                if (int.TryParse(no, out int packetId))
                {
                    if (client.Acks.ContainsKey(packetId))
                    {
                        var doc = JsonDocument.Parse(data);
                        var array = doc.RootElement.EnumerateArray().ToList();
                        var response = new SocketIOResponse(array, client);
                        client.Acks[packetId](response);
                    }
                }
            }
        }
    }
}
