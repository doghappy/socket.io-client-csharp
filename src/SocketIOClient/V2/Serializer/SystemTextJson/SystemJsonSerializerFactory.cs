using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;
using SocketIOClient.Serializer;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public class SystemJsonSerializerFactory(IServiceProvider serviceProvider) : ISerializerFactory
{
    public ISerializer Create(EngineIO engineIO)
    {
        var factory = serviceProvider.GetRequiredService<IEngineIOMessageAdapterFactory>();
        var adapter = factory.Create(engineIO);
        return ActivatorUtilities.CreateInstance<SystemJsonSerializer>(serviceProvider, adapter);
    }
}