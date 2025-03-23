using Newtonsoft.Json.Linq;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

// ReSharper disable once InconsistentNaming
public class NewtonJsonEngineIO4MessageAdapter : IEngineIOMessageAdapter
{
    public ConnectedMessage DeserializeConnectedMessage(string text)
    {
        var message = new ConnectedMessage();
        var rawJson = DecapsulateNamespace(text, message);
        message.Sid = JObject.Parse(rawJson).Value<string>("sid");
        return message;
    }

    private static string DecapsulateNamespace(string text, INamespaceMessage message)
    {
        var index = text.IndexOf('{');
        if (index > 0)
        {
            message.Namespace = text.Substring(0, index - 1);
            text = text.Substring(index);
        }
        return text;
    }

    public ErrorMessage DeserializeErrorMessage(string text)
    {
        var message = new ErrorMessage();
        var rawJson = DecapsulateNamespace(text, message);
        message.Error = JObject.Parse(rawJson).Value<string>("message");
        return message;
    }
}