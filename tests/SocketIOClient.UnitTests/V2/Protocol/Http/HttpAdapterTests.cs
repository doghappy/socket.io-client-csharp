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

        await _httpAdapter.SendAsync(new HttpRequest
        {
            Uri = new Uri("http://localhost"),
        }, CancellationToken.None);

        await observer.Received().OnNextAsync(Arg.Any<ProtocolMessage>());
    }

    [Fact]
    public async Task SendHttpRequestAsync_ObserverBlocked100Ms_SendAsyncNotBlockedByObserver()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        observer.OnNextAsync(Arg.Any<ProtocolMessage>()).Returns(async _ => await Task.Delay(100));
        _httpAdapter.Subscribe(observer);

        var stopwatch = Stopwatch.StartNew();
        await _httpAdapter.SendAsync(new HttpRequest
        {
            Uri = new Uri("http://localhost"),
        }, CancellationToken.None);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20);
    }

    [Fact]
    public async Task SendHttpRequestAsync_UrlIsNotProvided_UseDefault()
    {
        _httpAdapter.Uri = new Uri("http://localhost/?transport=polling");

        await _httpAdapter.SendAsync(new HttpRequest(), CancellationToken.None);

        await _httpClient.Received()
            .SendAsync(
                Arg.Is<IHttpRequest>(r => r.Uri.AbsoluteUri.StartsWith("http://localhost/?transport=polling&t=")),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendHttpRequestAsync_UrlIsProvided_UseProvidedOne()
    {
        _httpAdapter.Uri = new Uri("http://localhost");

        await _httpAdapter.SendAsync(new HttpRequest
        {
            Uri = new Uri("http://localhost:8080"),
        }, CancellationToken.None);

        await _httpClient.Received()
            .SendAsync(Arg.Is<IHttpRequest>(r => r.Uri == new Uri("http://localhost:8080")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void IsReadyToSend_UriIsNotSet_ReturnsFalse()
    {
        _httpAdapter.IsReadyToSend.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://localhost:3000/socket.io/?EIO=4&transport=polling", false)]
    [InlineData("http://localhost:3000/socket.io/?sidd=4", false)]
    [InlineData("http://localhost:3000/socket.io/?test=sid", false)]
    [InlineData("http://localhost:3000/socket.io/?sid=", true)]
    [InlineData("http://localhost:3000/socket.io/?EIO=4&transport=polling&sid=123", true)]
    public void IsReadyToSend_ReturnFalseByDefault_ReturnsTrueIfUriContainsSid(string uri, bool isReadyToSend)
    {
        _httpAdapter.Uri = new Uri(uri);
        _httpAdapter.IsReadyToSend.Should().Be(isReadyToSend);
    }

    [Fact]
    public async Task SendAsync_ResponseIsText_ObserverCanGetSameText()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);
        var httpResponse = Substitute.For<IHttpResponse>();
        httpResponse.ReadAsStringAsync().Returns("Hello World");
        _httpClient.SendAsync(Arg.Any<IHttpRequest>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        await _httpAdapter.SendAsync(new HttpRequest
        {
            Uri = new Uri("http://localhost"),
        }, CancellationToken.None);

        await observer.Received()
            .OnNextAsync(Arg.Is<ProtocolMessage>(m =>
                m.Type == ProtocolMessageType.Text
                && m.Text == "Hello World"));
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("Application/Octet-Stream")]
    [InlineData("APPLICATION/OCTET-STREAM")]
    public async Task SendAsync_ResponseIsBytes_ObserverCanGetFormattedBytes(string contentType)
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);
        var httpResponse = Substitute.For<IHttpResponse>();
        httpResponse.MediaType.Returns(contentType);
        httpResponse.ReadAsByteArrayAsync().Returns([1, 2, 255, 4, 3]);
        _httpClient.SendAsync(Arg.Any<IHttpRequest>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        await _httpAdapter.SendAsync(new HttpRequest
        {
            Uri = new Uri("http://localhost"),
        }, CancellationToken.None);

        await observer.Received()
            .OnNextAsync(Arg.Is<ProtocolMessage>(m => m.Type == ProtocolMessageType.Bytes));
    }
}