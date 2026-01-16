using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

public interface ISystemJsonEventMessage : ISystemJsonAckMessage, IEventMessage
{
}