using System.Collections.Generic;
using SocketIOClient.Common;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Session.EngineIOAdapter;

namespace SocketIOClient.Session.Http.EngineIOAdapter;

public interface IHttpEngineIOAdapter : IEngineIOAdapter
{
    HttpRequest ToHttpRequest(ICollection<byte[]> bytes);
    HttpRequest ToHttpRequest(string content);
    IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text);
    IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes);
}