using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.Core;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2;

public static class ServicesInitializer
{
    // TODO: test cases
    public static IServiceProvider BuildServiceProvider(IServiceCollection services, Action<IServiceCollection> configure = null)
    {
        services.AddLogging();
        services.AddSingleton<IStopwatch, SystemStopwatch>();
        services.AddSingleton<IRandom, SystemRandom>();
        services.AddSingleton<IDecapsulable, Decapsulator>();
        services.AddSingleton<IHttpClient, SystemHttpClient>();
        services.AddSingleton<IRetriable, RandomDelayRetryPolicy>();
        services.AddSingleton<IHttpAdapter, HttpAdapter>();
        services.AddSingleton<IUriConverter>(new DefaultUriConverter());

        // SystemTextJson or NewtonsoftJson
        // v3 or V4
        // Polling or WebSocket
        AddSystemTextJson(services);

        services.AddSingleton<IEngineIOAdapterFactory, EngineIOAdapterFactory>();
        services.AddSingleton<HttpMessageInvoker, HttpClient>();
        services.AddTransient<ISessionFactory, HttpSessionFactory>();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static void AddSystemTextJson(IServiceCollection services)
    {
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO3MessageAdapter>(EngineIO.V3);
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO4MessageAdapter>(EngineIO.V4);
        services.AddSingleton<IEngineIOMessageAdapterFactory, EngineIOMessageAdapterFactory>();
        services.AddSingleton<ISerializerFactory, SystemJsonSerializerFactory>();
    }
}