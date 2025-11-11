using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SocketIOClient.V2;
using SocketIOClient.V2.Session;

namespace SocketIOClient.UnitTests.V2;

public class ServicesInitializerTests
{
    [Fact]
    public void BuildServiceProvider_GetRequiredSessionIn1Scope_ReturnSameInstance()
    {
        var services = new ServiceCollection();
        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope = sp.CreateScope();
        var session1 = scope.ServiceProvider.GetRequiredService<ISession>();
        var session2 = scope.ServiceProvider.GetRequiredService<ISession>();
        session1.Should().Be(session2);
    }

    [Fact]
    public void BuildServiceProvider_GetRequiredSessionIn2Scope_Return2DifferentInstance()
    {
        var services = new ServiceCollection();
        var sp = ServicesInitializer.BuildServiceProvider(services);

        using var scope1 = sp.CreateScope();
        var session1 = scope1.ServiceProvider.GetRequiredService<ISession>();
        using var scope2 = sp.CreateScope();
        var session2 = scope2.ServiceProvider.GetRequiredService<ISession>();
        session1.Should().NotBe(session2);
    }
}