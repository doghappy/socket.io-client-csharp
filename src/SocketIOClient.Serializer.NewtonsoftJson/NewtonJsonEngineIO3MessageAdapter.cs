using Newtonsoft.Json.Linq;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

// ReSharper disable once InconsistentNaming
public class NewtonJsonEngineIO3MessageAdapter : IEngineIOMessageAdapter
{
    public ConnectedMessage DeserializeConnectedMessage(string text)
    {
        var message = new ConnectedMessage();
        if (!string.IsNullOrEmpty(text))
        {
            message.Namespace = text.TrimEnd(',');
        }
        return message;
    }

    public ErrorMessage DeserializeErrorMessage(string text)
    {
        var error = JToken.Parse(text).ToObject<string>();
        return new ErrorMessage
        {
            Error = error,
        };
    }
}