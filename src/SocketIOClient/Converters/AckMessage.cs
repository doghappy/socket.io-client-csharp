using System.Text;
using System.Text.Json;

namespace SocketIOClient.Converters
{
    public class AckMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.MessageAck;

        public string Namespace { get; set; }

        public JsonElement Json { get; set; }

        public int Id { get; set; }

        public void Read(string msg)
        {
            int index = msg.IndexOf('[');
            int lastIndex = msg.LastIndexOf(',', index);
            if (lastIndex > -1)
            {
                string text = msg.Substring(0, index);
                Namespace = text.Substring(0, lastIndex);
                Id = int.Parse(text.Substring(lastIndex + 1));
            }
            else
            {
                Id = int.Parse(msg.Substring(0, index));
            }
            msg = msg.Substring(index);
            Json = JsonDocument.Parse(msg).RootElement;
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder.Append("42").Append(Id);
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            builder.Append(Json.GetRawText());
            return builder.ToString();
        }
    }
}
