using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.NewtonJson;

public class WebSocketEngineIO3Tests(ITestOutputHelper output) : SystemJson.WebSocketEngineIO3Tests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}