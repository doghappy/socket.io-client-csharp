using System.Text;
using System.Text.Json;

namespace SocketIOClient.Messages
{
    public class ConnectedMessage : IMessage
    {
        public MessageType Type => MessageType.Connected;

        public string Namespace { get; set; }

        public string Sid { get; set; }

        public void Read(string msg)
        {
            int index = msg.IndexOf('{');
            if (index > 0)
            {
                Namespace = msg.Substring(0, index - 1);
                msg = msg.Substring(index);
            }
            else
            {
                Namespace = string.Empty;
            }
            Sid = JsonDocument.Parse(msg).RootElement.GetProperty("sid").GetString();
        }

        public void Eio3WsRead(string msg)
        {
            Namespace = msg.TrimEnd(',');
        }

        public void Eio3HttpRead(string msg)
        {

        }

        public string Write()
        {
            var builder = new StringBuilder("40");
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            return builder.ToString();
        }

        public string Eio3WsWrite()
        {
            throw new System.NotImplementedException();
        }

        public string Eio3HttpWrite()
        {
            throw new System.NotImplementedException();
        }
    }
}
