using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json;

public interface IEngineIOMessageAdapter
{
    ConnectedMessage DeserializeConnectedMessage(string text);
    ErrorMessage DeserializeErrorMessage(string text);
}