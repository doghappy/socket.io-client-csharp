using System.Collections.Generic;
using SocketIOClient.Core;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.Http.HttpEngineIOAdapter;

public interface IHttpEngineIOAdapter : IEngineIOAdapter
{
    HttpRequest ToHttpRequest(ICollection<byte[]> bytes);
    HttpRequest ToHttpRequest(string content);
    IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text);
    IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes);
}