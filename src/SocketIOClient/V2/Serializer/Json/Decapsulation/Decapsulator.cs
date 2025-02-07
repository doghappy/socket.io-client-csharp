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
}