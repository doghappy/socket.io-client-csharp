namespace SocketIOClient.Core.Messages;

public interface IEventMessage : IDataMessage
{
    public string Event { get; set; }
}