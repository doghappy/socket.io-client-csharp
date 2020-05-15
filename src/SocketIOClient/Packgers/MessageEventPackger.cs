using Newtonsoft.Json.Linq;

namespace SocketIOClient.Packgers
{
    public class MessageEventPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            if (!string.IsNullOrEmpty(client.Namespace) && text.StartsWith(client.Namespace))
            {
                text = text.Substring(client.Namespace.Length);
            }
            var array = JArray.Parse(text);
            string eventName = array[0].ToString();
            if (client.Handlers.ContainsKey(eventName))
            {
                array.RemoveAt(0);
                client.Handlers[eventName](new SocketIOResponse(array));
            }
        }
    }
}
