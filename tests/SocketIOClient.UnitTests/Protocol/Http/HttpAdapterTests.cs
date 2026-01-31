using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.Common;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Test.Core;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Protocol.Http;

public class HttpAdapterTests
{
    public HttpAdapterTests(ITestOutputHelper output)
    {
        _httpClient = Substitute.For<IHttpClient>();
        var logger = output.CreateLogger<HttpAdapter>();
        _httpAdapter = new HttpAdapter(_httpClient, logger);
    }

    private readonly HttpAdapter _httpAdapter;
    private readonly IHttpClient _httpClient;

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
                Arg.Is<HttpRequest>(r => r.Uri.AbsoluteUri.StartsWith("http://localhost/?transport=polling&t=")),
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
            .SendAsync(Arg.Is<HttpRequest>(r => r.Uri == new Uri("http://localhost:8080")), Arg.Any<CancellationToken>());
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

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData(null)]
    public async Task SendAsync_ResponseIsText_ObserverCanGetSameText(string mediaType)
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);
        var httpResponse = Substitute.For<IHttpResponse>();
        httpResponse.MediaType.Returns(mediaType);
        httpResponse.ReadAsStringAsync().Returns("Hello World");
        _httpClient.SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
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
        _httpClient.SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        await _httpAdapter.SendAsync(new HttpRequest
        {
            Uri = new Uri("http://localhost"),
        }, CancellationToken.None);

        await observer.Received()
            .OnNextAsync(Arg.Is<ProtocolMessage>(m => m.Type == ProtocolMessageType.Bytes));
    }

    [Fact]
    public void SetDefaultHeader_WhenCalled_AlwaysCallHttpClientSetDefaultHeader()
    {
        _httpAdapter.SetDefaultHeader("name", "value");

        _httpClient.Received().SetDefaultHeader("name", "value");
    }
}