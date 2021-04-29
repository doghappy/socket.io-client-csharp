using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Packgers
{
    public class MessageBinaryEventPackger : IUnpackable, IReceivedEvent
    {
        public string EventName { get; private set; }
        public SocketIOResponse Response { get; private set; }

        int _totalCount;
        List<JsonElement> _array;
        public event Action OnEnd;

        public void Unpack(SocketIO client, string text)
        {
            int index = text.IndexOf('-');
            if (index > 0)
            {
                if (int.TryParse(text.Substring(0, index), out _totalCount))
                {
                    text = text.Substring(index + 1);
                    if (!string.IsNullOrEmpty(client.Namespace) && text.StartsWith(client.Namespace))
                    {
                        text = text.Substring(client.Namespace.Length);
                    }
                    int packetIndex = text.IndexOf('[');
                    string id = null;
                    if (packetIndex > 0)
                    {
                        id = text.Substring(0, packetIndex);
                        text = text.Substring(packetIndex);
                    }
                    var doc = JsonDocument.Parse(text);
                    _array = doc.RootElement.EnumerateArray().ToList();
                    EventName = _array[0].GetString();
                    _array.RemoveAt(0);
                    Response = new SocketIOResponse(_array, client);
                    if (int.TryParse(id, out int packetId))
                    {
                        Response.PacketId = packetId;
                    }
                    client.OnBytesReceived += Client_OnBytesReceived;
                }
            }

        }

        private void Client_OnBytesReceived(object sender, byte[] e)
        {
            Response.InComingBytes.Add(e);
            if (Response.InComingBytes.Count == _totalCount)
            {
                var client = sender as SocketIO;
                if (client.Handlers.ContainsKey(EventName))
                {
                    client.Handlers[EventName](Response);
                }
                client.OnBytesReceived -= Client_OnBytesReceived;
                OnEnd();
            }
        }
    }
}
