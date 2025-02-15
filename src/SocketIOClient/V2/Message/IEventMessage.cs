namespace SocketIOClient.V2.Message;

public interface IEventMessage : IAckMessage
{
    public string Event { get; set; }
}