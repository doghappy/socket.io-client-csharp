using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Packgers
{
    public class MessageBinaryAckPackger : IUnpackable
    {
        int _totalCount;
        List<JsonElement> _array;
        SocketIOResponse _response;
        int _packetId;

        public void Unpack(SocketIO client, string text)
        {
            int index = text.IndexOf('-');
            if (index > 0)
            {
                if (int.TryParse(text.Substring(0, index), out _totalCount))
                {
                    text = text.Substring(index + 1);
                    if (!string.IsNullOrEmpty(client.Namespace))
                    {
                        text = text.Substring(client.Namespace.Length);
                    }
                    int packetIndex = text.IndexOf('[');
                    if (int.TryParse(text.Substring(0, packetIndex), out _packetId))
                    {
                        string data = text.Substring(packetIndex);
                        var doc = JsonDocument.Parse(data);
                        _array = doc.RootElement.EnumerateArray().ToList();
                        if (client.Acks.ContainsKey(_packetId))
                        {
                            _response = new SocketIOResponse(_array, client);
                            client.OnBytesReceived += Client_OnBytesReceived;
                        }
                    }
                }
            }

        }

        private void Client_OnBytesReceived(object sender, byte[] e)
        {
            _response.InComingBytes.Add(e);
            if (_response.InComingBytes.Count == _totalCount)
            {
                var client = sender as SocketIO;
                client.Acks[_packetId](_response);
                client.OnBytesReceived -= Client_OnBytesReceived;
            }
        }
    }
}
