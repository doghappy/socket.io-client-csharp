using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.Common.Messages;

namespace SocketIOClient.Serializer.SystemTextJson;

// ReSharper disable once InconsistentNaming
public class SystemJsonEngineIO3MessageAdapter : IEngineIOMessageAdapter
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

    private static string DecapsulateNamespace(string text, INamespaceMessage message)
    {
        var index = text.IndexOf('"');
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
        message.Error = JsonNode.Parse(rawJson).Deserialize<string>()!;
        return message;
    }
}