using System.Text.Json;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

// ReSharper disable once InconsistentNaming
public class SystemJsonEngineIO4MessageAdapter : IEngineIOMessageAdapter
{
    public ConnectedMessage DeserializeConnectedMessage(string text)
    {
        var message = new ConnectedMessage();
        var rawJson = DecapsulateNamespace(text, message);
        message.Sid = JsonDocument.Parse(rawJson).RootElement.GetProperty("sid").GetString();
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
        message.Error = JsonDocument.Parse(rawJson).RootElement.GetProperty("message").GetString();
        return message;
    }
}