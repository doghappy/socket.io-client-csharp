using System.Collections.Generic;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public interface IEngineIOAdapter: IMyAsyncObserver<IHttpResponse>, IMyObservable<string>, IMyObservable<byte[]>
{
    IHttpRequest ToHttpRequest(ICollection<byte[]> bytes);
    IHttpRequest ToHttpRequest(string content);
}