using System.Text;
using System.Text.Json;

namespace SocketIOClient.Converters
{
    public class BinaryAckMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.MessageBinaryAck;

        public string Namespace { get; set; }

        public JsonElement Json { get; set; }

        public int Id { get; set; }

        public int BinaryCount { get; set; }

        public void Read(string msg)
        {
            //461-0[{"_placeholder":true,"num":0}]
            //461-/nsp,0[{"_placeholder":true,"num":0}]

            int index1 = msg.IndexOf('-');
            BinaryCount = int.Parse(msg.Substring(0, index1));

            int index2 = msg.IndexOf('[');

            int index3 = msg.LastIndexOf(',', index2);
            if (index3 > -1)
            {
                Namespace = msg.Substring(index1 + 1, index3 - index1 - 1);
                Id = int.Parse(msg.Substring(index3 + 1, index2 - index3 - 1));
            }
            else
            {
                Id = int.Parse(msg.Substring(index1 + 1, index2 - index1 - 1));
            }

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
            builder
                .Append(Id)
                .Append(Json.GetRawText());
            return builder.ToString();
        }
    }
}
