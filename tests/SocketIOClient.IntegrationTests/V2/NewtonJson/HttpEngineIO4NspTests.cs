using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.NewtonJson;

public class HttpEngineIO4NspTests(ITestOutputHelper output) : SystemJson.HttpEngineIO4NspTests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}