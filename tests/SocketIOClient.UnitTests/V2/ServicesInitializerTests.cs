using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.Core;
using SocketIOClient.Serializer;
using SocketIOClient.V2;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.Http.EngineIOAdapter;
using SocketIOClient.V2.Session.WebSocket.EngineIOAdapter;

namespace SocketIOClient.UnitTests.V2;

public class ServicesInitializerTests
{
    [Fact]
    public void BuildServiceProvider_GetRequiredHttpSessionIn1Scope_ReturnSameInstance()
    {
        var services = new ServiceCollection();
        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope = sp.CreateScope();
        var session1 = scope.ServiceProvider.GetRequiredKeyedService<ISession>(TransportProtocol.Polling);
        var session2 = scope.ServiceProvider.GetRequiredKeyedService<ISession>(TransportProtocol.Polling);
        session1.Should().Be(session2);
    }

    [Fact]
    public void BuildServiceProvider_GetRequiredHttpSessionIn2Scope_Return2DifferentInstance()
    {
        var services = new ServiceCollection();
        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope1 = sp.CreateScope();
        var session1 = scope1.ServiceProvider.GetRequiredKeyedService<ISession>(TransportProtocol.Polling);
        using var scope2 = sp.CreateScope();
        var session2 = scope2.ServiceProvider.GetRequiredKeyedService<ISession>(TransportProtocol.Polling);
        session1.Should().NotBe(session2);
    }

    [Fact]
    public void BuildServiceProvider_WhenCalled_DefaultSerializerIsSystemTextJson()
    {
        var services = new ServiceCollection();

        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope = sp.CreateScope();
        var serializer = scope.ServiceProvider.GetRequiredService<ISerializer>();
        serializer.Should().BeOfType<SystemJsonSerializer>();
        var options = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        options.PropertyNamingPolicy.Should().BeNull();
    }

    [Fact]
    public void AddSystemTextJson_CustomOptions_OverrideDefaults()
    {
        var services = new ServiceCollection();

        var sp = ServicesInitializer.BuildServiceProvider(services, s =>
        {
            s.AddSystemTextJson(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        });

        using var scope = sp.CreateScope();
        var serializer = scope.ServiceProvider.GetRequiredService<ISerializer>();
        serializer.Should().BeOfType<SystemJsonSerializer>();
        var options = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Theory]
    [InlineData(EngineIOCompatibility.HttpEngineIO3)]
    [InlineData(EngineIOCompatibility.HttpEngineIO4)]
    public void BuildServiceProvider_ResolveIHttpEngineIOAdapter_AlwaysPass(EngineIOCompatibility compatibility)
    {
        var services = new ServiceCollection();

        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope = sp.CreateScope();
        var adapter = scope.ServiceProvider
            .GetRequiredKeyedService<IHttpEngineIOAdapter>(compatibility);

        adapter.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EngineIOCompatibility.WebSocketEngineIO3)]
    [InlineData(EngineIOCompatibility.WebSocketEngineIO4)]
    public void BuildServiceProvider_ResolveIWebSocketEngineIOAdapter_AlwaysPass(EngineIOCompatibility compatibility)
    {
        var services = new ServiceCollection();

        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope = sp.CreateScope();
        var adapter = scope.ServiceProvider
            .GetRequiredKeyedService<IWebSocketEngineIOAdapter>(compatibility);

        adapter.Should().NotBeNull();
    }
}