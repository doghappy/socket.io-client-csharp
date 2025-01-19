namespace SocketIOClient.V2.WebSocket;

public enum WebSocketMessageType
{
    Text,
    Binary,
    Close
}

public interface IWebSocketMessage
{
    public int Count { get; set; }
    public bool EndOfMessage { get; set; }
    public WebSocketMessageType Type { get; set; }
    public byte[] Bytes { get; set; }
}