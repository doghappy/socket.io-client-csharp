using System.Collections.Generic;
using SocketIOClient.Core;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public interface IEngineIOAdapter
{
    IHttpRequest ToHttpRequest(ICollection<byte[]> bytes);
    IHttpRequest ToHttpRequest(string content);
    IEnumerable<ProtocolMessage> GetMessages(string text);
}