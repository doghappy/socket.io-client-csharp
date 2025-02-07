using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.System;

public class SystemJsonEngineIO3MessageAdapter : IEngineIOMessageAdapter
{
    public ConnectedMessage DeserializeConnectedMessage(string text)
    {
        var message = new ConnectedMessage();
        if (text.Length < 2)
            return message;
        var startIndex = text.IndexOf('/');
        if (startIndex == -1)
            return message;
        
        var endIndex = text.IndexOf('?', startIndex);
        if (endIndex == -1)
        {
            endIndex = text.IndexOf(',', startIndex);
        }
        
        if (endIndex == -1)
        {
            endIndex = text.Length;
        }
        
        message.Namespace = text.Substring(startIndex, endIndex);
        return message;
    }
}