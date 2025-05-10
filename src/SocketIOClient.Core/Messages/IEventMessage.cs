namespace SocketIOClient.Core.Messages;

public interface IEventMessage : IAckMessage
{
    public string Event { get; set; }
}