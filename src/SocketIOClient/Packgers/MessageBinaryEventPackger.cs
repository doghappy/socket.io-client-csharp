using Newtonsoft.Json.Linq;

namespace SocketIOClient.Packgers
{
    public class MessageBinaryEventPackger : IUnpackable
    {
        int _totalCount;
        JArray _array;
        string _event;
        SocketIOResponse _response;

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
                    _event = _array[0].ToString();
                    if (client.Handlers.ContainsKey(_event))
                    {
                        _array.RemoveAt(0);
                        _response = new SocketIOResponse(_array);
                        client.OnBytesReceived += Client_OnBytesReceived;
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
                client.Handlers[_event](_response);
                client.OnBytesReceived -= Client_OnBytesReceived;
            }
        }
    }
}
