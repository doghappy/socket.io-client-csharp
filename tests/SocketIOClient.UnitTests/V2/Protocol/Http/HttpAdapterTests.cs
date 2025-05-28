using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.Core;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.UnitTests.V2.Protocol.Http;

public class HttpAdapterTests
{
    public HttpAdapterTests()
    {
        _httpClient = Substitute.For<IHttpClient>();
        _httpAdapter = new HttpAdapter(_httpClient);
    }

    private readonly HttpAdapter _httpAdapter;
    private readonly IHttpClient _httpClient;

    [Fact]
    public async Task SendProtocolMessageAsync_WhenCalled_OnNextShouldBeTriggered()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);

        await _httpAdapter.SendAsync(new ProtocolMessage(), CancellationToken.None);

        await observer.Received().OnNextAsync(Arg.Any<ProtocolMessage>());
    }

    [Fact]
    public async Task SendHttpRequestAsync_WhenCalled_OnNextShouldBeTriggered()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);

        await _httpAdapter.SendAsync(new HttpRequest(), CancellationToken.None);

        await observer.Received().OnNextAsync(Arg.Any<ProtocolMessage>());
    }

    [Fact]
    public async Task SendHttpRequestAsync_ObserverBlocked100Ms_SendAsyncNotBlockedByObserver()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        observer.OnNextAsync(Arg.Any<ProtocolMessage>()).Returns(async _ => await Task.Delay(100));
        _httpAdapter.Subscribe(observer);

        var stopwatch = Stopwatch.StartNew();
        await _httpAdapter.SendAsync(new HttpRequest(), CancellationToken.None);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20);
    }
}