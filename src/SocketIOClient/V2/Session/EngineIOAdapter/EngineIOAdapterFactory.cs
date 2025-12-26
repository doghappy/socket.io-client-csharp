using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public class EngineIOAdapterFactory(IServiceProvider serviceProvider) : IEngineIOAdapterFactory
{
    public IEngineIOAdapter Create(EngineIOCompatibility compatibility)
    {
        return serviceProvider.GetRequiredKeyedService<IEngineIOAdapter>(compatibility);
    }
}