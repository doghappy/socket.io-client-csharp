using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;

namespace SocketIOClient.Session.EngineIOAdapter;

public class EngineIOAdapterFactory(IServiceProvider serviceProvider) : IEngineIOAdapterFactory
{
    public T Create<T>(EngineIOCompatibility compatibility) where T : IEngineIOAdapter
    {
        return serviceProvider.GetRequiredKeyedService<T>(compatibility);
    }
}