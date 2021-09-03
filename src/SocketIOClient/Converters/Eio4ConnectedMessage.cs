using System.Text;
using System.Text.Json;

namespace SocketIOClient.Converters
{
    public class Eio4ConnectedMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.Connected;

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

        public string Write()
        {
            var builder = new StringBuilder("40");
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            return builder.ToString();
        }
    }
}
