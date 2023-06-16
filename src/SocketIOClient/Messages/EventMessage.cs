using SocketIOClient.JsonSerializer;
using SocketIOClient.Transport;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Messages
{
    public class EventMessage<T> : IJsonMessage<T>
    {
        public MessageType Type => MessageType.EventMessage;

        public string Namespace { get; set; }

        public string Event { get; set; }

        public int Id { get; set; }

        public List<T> JsonElements { get; set; }

        public string Json { get; set; }

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public EngineIO EIO { get; set; }

        public TransportProtocol Protocol { get; set; }
        public IJsonSerializer Serializer { get; set; }

        public void Read(string msg)
        {
            int index = msg.IndexOf('[');
            int lastIndex = msg.LastIndexOf(',', index);
            if (lastIndex > -1)
            {
                string text = msg.Substring(0, index);
                Namespace = text.Substring(0, lastIndex);
                if (index - lastIndex > 1)
                {
                    Id = int.Parse(text.Substring(lastIndex + 1));
                }
            }
            else
            {
                if (index > 0)
                {
                    Id = int.Parse(msg.Substring(0, index));
                }
            }
            msg = msg.Substring(index);

            //int index = msg.IndexOf('[');
            //if (index > 0)
            //{
            //    Namespace = msg.Substring(0, index - 1);
            //    msg = msg.Substring(index);
            //}
            var array = Serializer.GetListOfElementsFromRoot<T>(msg);
            Event = Serializer.GetString(array[0]);
            JsonElements = array.GetRange(1, array.Count - 1);
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder.Append("42");
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
