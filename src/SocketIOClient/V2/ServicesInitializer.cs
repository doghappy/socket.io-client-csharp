using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.Session.Http;
using SocketIOClient.V2.Session.Http.EngineIOAdapter;
using SocketIOClient.V2.Session.WebSocket;
using SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.V2;

public static class ServicesInitializer
{
    public static IServiceProvider BuildServiceProvider(IServiceCollection services, Action<IServiceCollection> configure = null)
    {
        services.AddLogging();
        services
            .AddSingleton<IStopwatch, SystemStopwatch>()
            .AddSingleton<IRandom, SystemRandom>()
            .AddSingleton<IDecapsulable, Decapsulator>()
            .AddSingleton<IRetriable, RandomDelayRetryPolicy>()
            .AddSingleton<IUriConverter, DefaultUriConverter>();

        services
            .AddEngineIOCompatibility()
            .AddHttpSession()
            .AddWebSocketSession()
            .AddSystemTextJson(new JsonSerializerOptions());

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddEngineIOCompatibility(this IServiceCollection services)
    {
        services.AddScoped<IPollingHandler, PollingHandler>();
        services.AddScoped<IEngineIOAdapterFactory, EngineIOAdapterFactory>();
        services.AddKeyedScoped<IEngineIOAdapter, HttpEngineIO3Adapter>(EngineIOCompatibility.HttpEngineIO3);
        services.AddKeyedScoped<IEngineIOAdapter, HttpEngineIO4Adapter>(EngineIOCompatibility.HttpEngineIO4);
        services.AddKeyedScoped<IEngineIOAdapter, WebSocketEngineIO3Adapter>(EngineIOCompatibility.WebSocketEngineIO3);
        services.AddKeyedScoped<IEngineIOAdapter, WebSocketEngineIO4Adapter>(EngineIOCompatibility.WebSocketEngineIO4);
        return services;
    }

    private static IServiceCollection AddWebSocketSession(this IServiceCollection services)
    {
        services.AddScoped<IWebSocketClient, SystemClientWebSocket>();
        services.AddScoped<IWebSocketAdapter, WebSocketAdapter>();
        services.AddScoped<IWebSocketClientAdapter, SystemClientWebSocketAdapter>();
        services.AddKeyedScoped<ISession, WebSocketSession>(TransportProtocol.WebSocket);
        return services;
    }

    private static IServiceCollection AddHttpSession(this IServiceCollection services)
    {
        services.AddSingleton<IHttpClient, SystemHttpClient>();
        services.AddScoped<IHttpAdapter, HttpAdapter>();
        services.AddSingleton<HttpClient>();
        services.AddKeyedScoped<ISession, HttpSession>(TransportProtocol.Polling);
        return services;
    }

    public static IServiceCollection AddSystemTextJson(this IServiceCollection services, JsonSerializerOptions options)
    {
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO3MessageAdapter>(EngineIO.V3);
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO4MessageAdapter>(EngineIO.V4);
        services.AddSingleton<IEngineIOMessageAdapterFactory, EngineIOMessageAdapterFactory>();
        services.AddSingleton<ISerializer, SystemJsonSerializer>();
        services.AddSingleton(options);
        return services;
    }
}
