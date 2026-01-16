using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;

namespace SocketIOClient.Serializer.SystemTextJson;

public class SystemJsonSerializerFactory(IServiceProvider serviceProvider) : ISerializerFactory
{
    public ISerializer Create(EngineIO engineIO)
    {
        var adapter = serviceProvider.GetRequiredKeyedService<IEngineIOMessageAdapter>(engineIO);
        return ActivatorUtilities.CreateInstance<SystemJsonSerializer>(serviceProvider, adapter);
    }
}