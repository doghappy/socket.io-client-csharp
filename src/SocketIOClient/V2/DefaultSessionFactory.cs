using System.Net.Http;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2;

public class DefaultSessionFactory : ISessionFactory
{
    public ISession New(EngineIO eio)
    {
        var engineIOAdapter = NewEnginIOAdapter(eio);
        var httpClient = new SystemHttpClient(new HttpClient());
        return new HttpSession(
            engineIOAdapter,
            new HttpAdapter(httpClient),
            new SystemJsonSerializer(new Decapsulator()),
            new DefaultUriConverter((int)eio));
    }

    private static IEngineIOAdapter NewEnginIOAdapter(EngineIO eio)
    {
        if (eio == EngineIO.V3)
        {
            return new EngineIO3Adapter();
        }
        return new EngineIO4Adapter();
    }
}