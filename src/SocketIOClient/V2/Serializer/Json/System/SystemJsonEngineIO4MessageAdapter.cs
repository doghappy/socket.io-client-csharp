using System.Text.Json;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonEngineIO4MessageAdapter : IEngineIOMessageAdapter
{
    public ConnectedMessage DeserializeConnectedMessage(string text)
    {
        var message = new ConnectedMessage();

        var index = text.IndexOf('{');
        if (index > 0)
        {
            message.Namespace = text.Substring(0, index - 1);
            text = text.Substring(index);
        }

        message.Sid = JsonDocument.Parse(text).RootElement.GetProperty("sid").GetString();
        return message;
    }
}