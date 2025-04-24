using System.Net.Http;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2;

public interface ISessionFactory
{
    ISession New(EngineIO eio, SessionOptions options);
}

public class DefaultSessionFactory : ISessionFactory
{
    public ISession New(EngineIO eio, SessionOptions options)
    {
        var httpClient = new SystemHttpClient(new HttpClient());
        var httpAdapter = new HttpAdapter(httpClient);
        var serializer = new SystemJsonSerializer(new Decapsulator());
        var stopwatch = new SystemStopwatch();
        var random = new SystemRandom();
        var randomDelayRetryPolicy = new RandomDelayRetryPolicy(random);
        IEngineIOAdapter engineIOAdapter = eio == EngineIO.V3
            ? new EngineIO3Adapter(stopwatch, serializer, httpAdapter, options.Timeout, randomDelayRetryPolicy)
            : new EngineIO4Adapter(stopwatch, serializer, httpAdapter, options.Timeout, randomDelayRetryPolicy);
        return new HttpSession(
            options,
            engineIOAdapter,
            new HttpAdapter(httpClient),
            serializer,
            new DefaultUriConverter((int)eio));
    }
}