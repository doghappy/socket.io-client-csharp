using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2;

public static class ServicesInitializer
{
    public static IServiceProvider BuildServiceProvider(IServiceCollection services, Action<IServiceCollection> configure = null)
    {
        services.AddLogging();
        services.AddSingleton<IStopwatch, SystemStopwatch>();
        services.AddSingleton<IRandom, SystemRandom>();
        services.AddSingleton<IDecapsulable, Decapsulator>();
        services.AddSingleton<IHttpClient, SystemHttpClient>();
        services.AddSingleton<IRetriable, RandomDelayRetryPolicy>();
        services.AddScoped<IHttpAdapter, HttpAdapter>();
        services.AddSingleton<IUriConverter>(new DefaultUriConverter());

        // SystemTextJson or NewtonsoftJson
        // v3 or V4
        // Polling or WebSocket
        AddSystemTextJson(services);

        services.AddScoped<IEngineIOAdapterFactory, EngineIOAdapterFactory>();
        services.AddKeyedScoped<IEngineIOAdapter, EngineIO3Adapter>(EngineIO.V3);
        services.AddKeyedScoped<IEngineIOAdapter, EngineIO4Adapter>(EngineIO.V4);

        // TODO: Microsoft.Extensions.Http .AddHttpClient()
        services.AddSingleton<HttpClient>();
        // services.AddScoped<ISessionFactory, HttpSessionFactory>();
        services.AddScoped<ISession, HttpSession>();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static void AddSystemTextJson(IServiceCollection services)
    {
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO3MessageAdapter>(EngineIO.V3);
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO4MessageAdapter>(EngineIO.V4);
        services.AddSingleton<IEngineIOMessageAdapterFactory, EngineIOMessageAdapterFactory>();
        // TODO: Should be Scoped, need to add test cases to cover it
        services.AddSingleton<ISerializer, SystemJsonSerializer>();
    }
}

// public record ProtocolOptions
// {
//     public ProtocolOptions(TransportProtocol protocol, EngineIO engineIO)
//     {
//         Protocol = protocol;
//         EngineIO = engineIO;
//     }
//
//     public TransportProtocol Protocol { get; }
//     public EngineIO EngineIO { get; }
// }