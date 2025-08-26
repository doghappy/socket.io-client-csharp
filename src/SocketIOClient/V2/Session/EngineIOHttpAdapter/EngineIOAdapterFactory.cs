using System;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public class EngineIOAdapterFactory(IServiceProvider serviceProvider) : IEngineIOAdapterFactory
{
    public IEngineIOAdapter Create(EngineIO engineIO, EngineIOAdapterOptions options)
    {
        if (engineIO == EngineIO.V3)
        {
            return ActivatorUtilities.CreateInstance<EngineIO3Adapter>(serviceProvider, options);
        }
        return ActivatorUtilities.CreateInstance<EngineIO4Adapter>(serviceProvider, options);
    }
}