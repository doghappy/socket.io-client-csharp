using System.Collections.Generic;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public interface IEngineIOAdapter
{
    IHttpRequest ToHttpRequest(ICollection<byte[]> bytes);
}