using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.Messages
{
    public interface IJsonMessage : IMessage
    {
        List<JsonElement> JsonElements { get; }
    }
}