using System.Text;

namespace SocketIOClient.Messages
{
    public class ClientAckMessage : IMessage
    {
        public MessageType Type => MessageType.AckMessage;

        public string Namespace { get; set; }

        public string Json { get; set; }

        public int Id { get; set; }

        public void Read(string msg)
        {
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder.Append("43").Append(Id);
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            if (string.IsNullOrEmpty(Json))
            {
                builder.Append("[]");
            }
            else
            {
                builder.Append(Json);
            }
            return builder.ToString();
        }
    }
}
