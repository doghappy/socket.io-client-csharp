using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.NewtonJson;

public class HttpEngineIO4Tests(ITestOutputHelper output) : SystemJson.HttpEngineIO4Tests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}