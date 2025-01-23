namespace SocketIOClient.V2.Protocol;

public enum ProtocolMessageType
{
    Text,
    Bytes,
}

public class ProtocolMessage
{
    public ProtocolMessageType Type { get; set; }
    public string Text { get; set; }
    public byte[] Bytes { get; set; }
}