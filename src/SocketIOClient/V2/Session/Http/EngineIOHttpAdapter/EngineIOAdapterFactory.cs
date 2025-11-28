using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.Http.EngineIOHttpAdapter;

public class EngineIOAdapterFactory(IServiceProvider serviceProvider) : IEngineIOAdapterFactory
{
    public IEngineIOAdapter Create(EngineIO engineIO)
    {
        return serviceProvider.GetRequiredKeyedService<IEngineIOAdapter>(engineIO);
    }
}