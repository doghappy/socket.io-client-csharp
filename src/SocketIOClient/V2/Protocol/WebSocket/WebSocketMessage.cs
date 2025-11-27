namespace SocketIOClient.V2.Protocol.WebSocket;

public enum WebSocketMessageType
{
    Text,
    Binary,
    Close
}

public class WebSocketMessage
{
    public WebSocketMessageType Type { get; set; }
    public byte[] Bytes { get; set; }
}