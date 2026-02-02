using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SocketIOClient.Common;

namespace SocketIOClient.Serializer.NewtonsoftJson;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddNewtonsoftJson(this IServiceCollection services, JsonSerializerSettings settings)
    {
        services.AddKeyedSingleton<IEngineIOMessageAdapter, NewtonJsonEngineIO3MessageAdapter>(EngineIO.V3);
        services.AddKeyedSingleton<IEngineIOMessageAdapter, NewtonJsonEngineIO4MessageAdapter>(EngineIO.V4);
        services.AddSingleton<ISerializer, NewtonJsonSerializer>();
        services.AddSingleton(settings);
        return services;
    }

    public static IServiceCollection AddNewtonsoftJson(this IServiceCollection services)
    {
        return services.AddNewtonsoftJson(new JsonSerializerSettings());
    }
}