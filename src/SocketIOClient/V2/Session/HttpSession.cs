using System;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2.Session;

public class HttpSession(
    IEngineIOAdapter engineIOAdapter,
    IHttpAdapter httpAdapter,
    ISerializer serializer,
    IUriConverter uriConverter) : ISession
{
    public void OnNext(ProtocolMessage value)
    {
        throw new NotImplementedException();
    }

    public async Task SendAsync(IMessage message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }
        // TODO: serialize by message.Type
        // var protocolMessages = serializer.Serialize()
        await httpAdapter.SendAsync(null, cancellationToken);
        throw new NotImplementedException();
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}