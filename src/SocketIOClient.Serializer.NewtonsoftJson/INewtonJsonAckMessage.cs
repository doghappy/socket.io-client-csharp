using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public interface INewtonJsonAckMessage : IAckMessage
{
    JArray DataItems { get; set; }
    JsonSerializerSettings JsonSerializerSettings { get; set; }
}