using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer;

public interface IEngineIOMessageAdapter
{
    ConnectedMessage DeserializeConnectedMessage(string text);
    ErrorMessage DeserializeErrorMessage(string text);
}