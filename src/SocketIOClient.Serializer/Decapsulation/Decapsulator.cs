using System;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.Decapsulation;

public class Decapsulator : IDecapsulable
{
    public DecapsulationResult DecapsulateRawText(string text)
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

    public MessageResult DecapsulateEventMessage(string text)
    {
        var result = new MessageResult();
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

    public BinaryEventMessageResult DecapsulateBinaryEventMessage(string text)
    {
        var result = new BinaryEventMessageResult();
        var index1 = text.IndexOf('-');
        result.BytesCount = int.Parse(text.Substring(0, index1));

        var index2 = text.IndexOf('[');

        var index3 = text.LastIndexOf(',', index2);
        if (index3 > -1)
        {
            result.Namespace = text.Substring(index1 + 1, index3 - index1 - 1);
            var idLength = index2 - index3 - 1;
            if (idLength > 0)
            {
                result.Id = int.Parse(text.Substring(index3 + 1, idLength));
            }
        }
        else
        {
            var idLength = index2 - index1 - 1;
            if (idLength > 0)
            {
                result.Id = int.Parse(text.Substring(index1 + 1, idLength));
            }
        }

        result.Data = text.Substring(index2);
        return result;
    }
}