using Newtonsoft.Json.Linq;
using System;

namespace SocketIOClient.Packgers
{
    public class MessageBinaryEventPackger : IUnpackable, IReceivedEvent
    {
        public string EventName { get; private set; }
        public SocketIOResponse Response { get; private set; }

        int _totalCount;
        JArray _array;
        public event Action OnEnd;

        public void Unpack(SocketIO client, string text)
        {
            int index = text.IndexOf('-');
            if (index > 0)
            {
                if (int.TryParse(text.Substring(0, index), out _totalCount))
                {
                    string data = text.Substring(index + 1);
                    if (!string.IsNullOrEmpty(client.Namespace))
                    {
                        data = data.Substring(client.Namespace.Length);
                    }
                    _array = JArray.Parse(data);
                    EventName = _array[0].ToString();
                    _array.RemoveAt(0);
                    Response = new SocketIOResponse(_array);
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
