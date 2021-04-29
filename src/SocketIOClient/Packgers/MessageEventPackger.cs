using System;
using System.Linq;
using System.Text.Json;

namespace SocketIOClient.Packgers
{
    public class MessageEventPackger : IUnpackable, IReceivedEvent
    {
        public string EventName { get; private set; }
        public SocketIOResponse Response { get; private set; }

        public event Action OnEnd;

        public void Unpack(SocketIO client, string text)
        {
            if (!string.IsNullOrEmpty(client.Namespace) && text.StartsWith(client.Namespace))
            {
                text = text.Substring(client.Namespace.Length);
            }
            int index = text.IndexOf('[');
            string id = null;
            if (index > 0)
            {
                id = text.Substring(0, index);
                text = text.Substring(index);
            }
            var array = JsonDocument.Parse(text).RootElement.EnumerateArray().ToList();
            EventName = array[0].GetString();
            array.RemoveAt(0);
            Response = new SocketIOResponse(array, client);
            if (int.TryParse(id, out int packetId))
            {
                Response.PacketId = packetId;
            }
            if (client.Handlers.ContainsKey(EventName))
            {
                client.Handlers[EventName](Response);
            }
            OnEnd();
        }
    }
}
