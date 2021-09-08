using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SocketIOClient.Messages
{
    /// <summary>
    /// The server calls the client's callback
    /// </summary>
    public class ClientAckMessage : IMessage
    {
        public MessageType Type => MessageType.AckMessage;

        public string Namespace { get; set; }

        public string Event { get; set; }

        public List<JsonElement> JsonElements { get; set; }

        public string Json { get; set; }

        public int Id { get; set; }

        public ICollection<byte[]> OutgoingBytes { get; set; }

        public ICollection<byte[]> IncomingBytes { get; }

        public int BinaryCount { get; }

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
            JsonElements = JsonDocument.Parse(msg).RootElement.EnumerateArray().ToList();
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder.Append("42").Append(Id);
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            if (string.IsNullOrEmpty(Json))
            {
                builder.Append("[\"").Append(Event).Append("\"]");
            }
            else
            {
                string data = Json.Insert(1, $"\"{Event}\",");
                builder.Append(data);
            }
            return builder.ToString();
        }
    }
}
