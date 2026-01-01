using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.NewtonJson;

public class HttpEngineIO3Tests(ITestOutputHelper output) : SystemJson.HttpEngineIO3Tests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}