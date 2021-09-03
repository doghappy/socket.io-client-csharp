using System.Text;
using System.Text.Json;

namespace SocketIOClient.Converters
{
    public class EventMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.MessageEvent;

        public string Namespace { get; set; }

        public JsonElement Json { get; set; }

        public void Read(string msg)
        {
            int index = msg.IndexOf('[');
            if (index > 0)
            {
                Namespace = msg.Substring(0, index - 1);
                msg = msg.Substring(index);
            }
            Json = JsonDocument.Parse(msg).RootElement;
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder.Append("42");
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            builder.Append(Json.GetRawText());
            return builder.ToString();
        }
    }
}
