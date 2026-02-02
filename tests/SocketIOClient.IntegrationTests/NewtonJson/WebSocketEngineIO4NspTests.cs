using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.NewtonJson;

public class WebSocketEngineIO4NspTests(ITestOutputHelper output) : SystemJson.WebSocketEngineIO4NspTests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}