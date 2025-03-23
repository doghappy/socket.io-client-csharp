using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public interface INewtonJsonEventMessage : INewtonJsonAckMessage, IEventMessage
{
}