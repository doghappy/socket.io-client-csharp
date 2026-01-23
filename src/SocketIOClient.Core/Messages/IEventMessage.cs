namespace SocketIOClient.Core.Messages;

public interface IEventMessage : IDataMessage
{
    string Event { get; set; }
}