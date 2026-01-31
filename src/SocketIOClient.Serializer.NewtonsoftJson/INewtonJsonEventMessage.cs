using SocketIOClient.Common.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public interface INewtonJsonEventMessage : INewtonJsonAckMessage, IEventMessage
{
}