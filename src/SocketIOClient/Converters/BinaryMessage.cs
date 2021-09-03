using System.Text;
using System.Text.Json;

namespace SocketIOClient.Converters
{
    public class BinaryMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.MessageBinary;

        public string Namespace { get; set; }

        public JsonElement Json { get; set; }

        public int BinaryCount { get; set; }

        public void Read(string msg)
        {
            int index1 = msg.IndexOf('-');
            BinaryCount = int.Parse(msg.Substring(0, index1));

            int index2 = msg.IndexOf('[');
            Namespace = msg.Substring(index1 + 1, index2 - 2).TrimEnd(',');

            Json = JsonDocument.Parse(msg.Substring(index2)).RootElement;
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder
                .Append("45")
                .Append(BinaryCount)
                .Append('-');
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            builder.Append(Json.GetRawText());
            return builder.ToString();
        }
    }
}
