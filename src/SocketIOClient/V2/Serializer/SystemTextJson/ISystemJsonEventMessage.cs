using SocketIOClient.Core.Messages;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public interface ISystemJsonEventMessage : ISystemJsonAckMessage, IEventMessage
{
}