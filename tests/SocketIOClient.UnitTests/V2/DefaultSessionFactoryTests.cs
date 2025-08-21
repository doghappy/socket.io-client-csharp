using System.Reflection;
using FluentAssertions;
using SocketIOClient.V2;
using SocketIOClient.V2.Core;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;

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

    [Fact]
    public void New_HttpSession_HttpAdapterIsTheSameInstance()
    {
        var httpSession = (HttpSession)_sessionFactory.New(EngineIO.V3, new SessionOptions());
        var httpSessionType = httpSession.GetType();
        const BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        var httpAdapterField = httpSessionType.GetField("_httpAdapter", nonPublicInstance)!;
        var httpAdapterOfSession = (IHttpAdapter)httpAdapterField.GetValue(httpSession)!;

        var engineIOAdapterField = httpSessionType.GetField("_engineIOAdapter", nonPublicInstance)!;
        var engineIOAdapter = (EngineIO3Adapter)engineIOAdapterField.GetValue(httpSession)!;
        var httpAdapterOfEngineIOAdapter = engineIOAdapter
            .GetType()
            .GetField("_httpAdapter", nonPublicInstance)!
            .GetValue(engineIOAdapter)!;

        httpAdapterOfSession.Should().BeSameAs(httpAdapterOfEngineIOAdapter);
    }
}