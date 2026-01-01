using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.NewtonJson;

public class WebSocketEngineIO3NspTests(ITestOutputHelper output) : SystemJson.WebSocketEngineIO3NspTests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}