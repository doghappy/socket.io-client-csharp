using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;

namespace SocketIOClient.V2.Session;

public class HttpSessionFactory(IServiceProvider serviceProvider) : ISessionFactory
{
    public ISession Create(SessionOptions options)
    {
        var engineIOAdapterFactory = serviceProvider.GetRequiredService<IEngineIOAdapterFactory>();
        var engineIOAdapter = engineIOAdapterFactory.Create(options.EngineIO, new EngineIOAdapterOptions
        {
            Timeout = options.Timeout,
        });
        var serializerFactory = serviceProvider.GetRequiredService<ISerializerFactory>();
        var serializer = serializerFactory.Create(options.EngineIO);
        return ActivatorUtilities.CreateInstance<HttpSession>(serviceProvider, options, engineIOAdapter, serializer);
    }
}