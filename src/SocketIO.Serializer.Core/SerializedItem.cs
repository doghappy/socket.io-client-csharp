namespace SocketIO.Serializer.Core
{
    public class SerializedItem
    {
        public SerializedMessageType Type { get; set; }
        public string Text { get; set; }
        public byte[] Binary { get; set; }
    }
    
    public enum SerializedMessageType
    {
        Text,
        Binary,
    }
}