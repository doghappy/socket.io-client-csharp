namespace SocketIOClient.Common.Messages;

public interface IEventMessage : IDataMessage
{
    string Event { get; set; }
}