namespace SocketIOClient.Converters
{
    public class DisconnectedMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.Disconnected;

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
