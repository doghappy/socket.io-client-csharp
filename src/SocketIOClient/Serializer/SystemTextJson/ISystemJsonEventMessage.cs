using SocketIOClient.Common.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

public interface ISystemJsonEventMessage : ISystemJsonAckMessage, IEventMessage
{
}