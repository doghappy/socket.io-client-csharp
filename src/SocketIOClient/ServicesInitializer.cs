using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Common;
using SocketIOClient.Infrastructure;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Protocol.WebSocket;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.Serializer.SystemTextJson;
using SocketIOClient.Session;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.Http;
using SocketIOClient.Session.Http.EngineIOAdapter;
using SocketIOClient.Session.WebSocket;
using SocketIOClient.Session.WebSocket.EngineIOAdapter;

namespace SocketIOClient;

public static class ServicesInitializer
{
    public static IServiceProvider BuildServiceProvider(IServiceCollection services, Action<IServiceCollection>? configure = null)
    {
        services.AddLogging();
        services
            .AddSingleton<IStopwatch, SystemStopwatch>()
            .AddSingleton<IRandom, SystemRandom>()
            .AddSingleton<IDecapsulable, Decapsulator>()
            .AddSingleton<IRetriable, RandomDelayRetryPolicy>()
            .AddSingleton<IEngineIOMessageAdapterFactory, EngineIOMessageAdapterFactory>()
            .AddSingleton<IDelay, TaskDelay>();

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
        services.AddKeyedScoped<IHttpEngineIOAdapter, HttpEngineIO3Adapter>(EngineIOCompatibility.HttpEngineIO3);
        services.AddKeyedScoped<IHttpEngineIOAdapter, HttpEngineIO4Adapter>(EngineIOCompatibility.HttpEngineIO4);
        services.AddKeyedScoped<IWebSocketEngineIOAdapter, WebSocketEngineIO3Adapter>(EngineIOCompatibility.WebSocketEngineIO3);
        services.AddKeyedScoped<IWebSocketEngineIOAdapter, WebSocketEngineIO4Adapter>(EngineIOCompatibility.WebSocketEngineIO4);
        return services;
    }

    private static IServiceCollection AddWebSocketSession(this IServiceCollection services)
    {
        services.AddScoped<IWebSocketClient, SystemClientWebSocket>();
        services.AddScoped<IWebSocketAdapter, WebSocketAdapter>();
        services.AddScoped<IWebSocketClientAdapter, SystemClientWebSocketAdapter>();
        services.AddKeyedScoped<ISession, WebSocketSession>(TransportProtocol.WebSocket);
        services.AddSingleton(new WebSocketOptions());
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
        services.AddSingleton<ISerializer, SystemJsonSerializer>();
        services.AddSingleton(options);
        return services;
    }
}
