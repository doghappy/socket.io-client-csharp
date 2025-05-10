using FluentAssertions;
using SocketIOClient.V2;
using SocketIOClient.V2.Session;

namespace SocketIOClient.UnitTests.V2;

public class DefaultSessionFactoryTests
{
    public DefaultSessionFactoryTests()
    {
        _sessionFactory = new DefaultSessionFactory();
    }

    private readonly DefaultSessionFactory _sessionFactory;

    [Fact]
    public void New_OptionIsNull_ThrowNullReferenceException()
    {
        _sessionFactory.Invoking(x => x.New(EngineIO.V3, null))
            .Should()
            .ThrowExactly<NullReferenceException>();
    }

    [Fact]
    public void New_WhenCalled_AlwaysReturnHttpSession()
    {
        var session = _sessionFactory.New(EngineIO.V3, new SessionOptions());
        session.Should().BeOfType<HttpSession>();
    }
}