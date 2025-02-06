using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Session;

public class HttpSessionTests
{
    public HttpSessionTests()
    {
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _engineIOAdapter = Substitute.For<IEngineIOAdapter>();
        _serializer = Substitute.For<ISerializer>();
        _session = new HttpSession(_engineIOAdapter, _httpAdapter,_serializer, new DefaultUriConverter());
    }
    
    private readonly HttpSession _session;
    private readonly IHttpAdapter _httpAdapter;
    private readonly IEngineIOAdapter _engineIOAdapter;
    private readonly ISerializer _serializer;
    
    [Fact]
    public async Task SendAsync_GivenANull_ThrowArgumentNullException()
    {
        await _session.Invoking(async x => await x.SendAsync(null, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }
}