namespace SocketIOClient.Messages
{
    public class DisconnectedMessage : IMessage
    {
        public MessageType Type => MessageType.Disconnected;

        public string Namespace { get; set; }

        public void Read(string msg)
        {
            Namespace = msg.TrimEnd(',');
        }

        public string Write()
        {
            if (string.IsNullOrEmpty(Namespace))
            {
                return "41";
            }
            return "41" + Namespace + ",";
        }
    }
}
