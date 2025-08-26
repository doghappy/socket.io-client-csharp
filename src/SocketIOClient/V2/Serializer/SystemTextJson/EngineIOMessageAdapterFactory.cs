using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;
using SocketIOClient.Serializer;

namespace SocketIOClient.V2.Serializer.SystemTextJson;

public class EngineIOMessageAdapterFactory(
    [FromKeyedServices(EngineIO.V3)] IEngineIOMessageAdapter v3,
    [FromKeyedServices(EngineIO.V4)] IEngineIOMessageAdapter v4)
    : IEngineIOMessageAdapterFactory
{
    public IEngineIOMessageAdapter Create(EngineIO engineIO)
    {
        return engineIO == EngineIO.V3 ? v3 : v4;
    }
}