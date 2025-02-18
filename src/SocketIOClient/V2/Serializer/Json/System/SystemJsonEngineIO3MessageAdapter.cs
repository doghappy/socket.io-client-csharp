using System.Text.Json;
using System.Text.Json.Nodes;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

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

    public ErrorMessage DeserializeErrorMessage(string text)
    {
        var error = JsonNode.Parse(text).Deserialize<string>();
        return new ErrorMessage
        {
            Error = error,
        };
    }
}