using System;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Serializer.Json.Decapsulation;

public class Decapsulator: IDecapsulable
{
    public DecapsulationResult Decapsulate(string text)
    {
        var result = new DecapsulationResult();
        var enums = Enum.GetValues(typeof(MessageType));
        foreach (MessageType type in enums)
        {
            var prefix = ((int)type).ToString();
            if (!text.StartsWith(prefix)) continue;

            var data = text.Substring(prefix.Length);

            result.Success = true;
            result.Type = type;
            result.Data = data;
        }
        return result;
    }

    public EventMessageResult DecapsulateEventMessage(string text)
    {
        var result = new EventMessageResult();
        var index = text.IndexOf('[');
        var lastIndex = text.LastIndexOf(',', index);
        if (lastIndex > -1)
        {
            var subText = text.Substring(0, index);
            result.Namespace = subText.Substring(0, lastIndex);
            if (index - lastIndex > 1)
            {
                result.Id = int.Parse(subText.Substring(lastIndex + 1));
            }
        }
        else
        {
            if (index > 0)
            {
                result.Id = int.Parse(text.Substring(0, index));
            }
        }
        result.Data = text.Substring(index);
        return result;
    }
}