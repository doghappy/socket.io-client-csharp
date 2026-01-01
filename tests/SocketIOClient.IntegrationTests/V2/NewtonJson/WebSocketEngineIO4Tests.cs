using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Serializer.NewtonsoftJson;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.NewtonJson;

public class WebSocketEngineIO4Tests(ITestOutputHelper output) : SystemJson.WebSocketEngineIO4Tests(output)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddNewtonsoftJson();
    }
}