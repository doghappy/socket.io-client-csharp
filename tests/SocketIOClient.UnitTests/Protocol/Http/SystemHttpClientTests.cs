using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using SocketIOClient.Protocol.Http;

namespace SocketIOClient.UnitTests.Protocol.Http;

public class SystemHttpClientTests
{
    public SystemHttpClientTests()
    {
        _httpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<SystemHttpClient>>();
        _httpClient = new SystemHttpClient(_httpMessageHandler.ToHttpClient(), logger);
    }

    private readonly SystemHttpClient _httpClient;
    private readonly MockHttpMessageHandler _httpMessageHandler;

    [Fact]
    public async Task SendAsync_TextPlainWithTextBody_ReadAsStringAsyncReturnsSameBody()
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .Respond("text/plain", "Hello, Google!");

        var res = await _httpClient.SendAsync(new HttpRequest
        {
            Uri = new Uri("https://www.google.com"),
        }, CancellationToken.None);

        res.MediaType.Should().Be("text/plain");
        var body = await res.ReadAsStringAsync();
        body.Should().Be("Hello, Google!");
    }

    [Fact]
    public async Task SendAsync_PassACanceledToken_ThrowTaskCanceledException()
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .Respond("text/plain", "Hello, Google!");

        await _httpClient.Invoking(async x =>
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel(true);
                return await x.SendAsync(new HttpRequest
                {
                    Uri = new Uri("https://www.google.com"),
                }, cts.Token);
            })
            .Should()
            .ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task SendAsync_TextInByteArrayContent_BothMethodCanGetValue()
    {
        var bytes = "Test Zip"u8.ToArray();
        _httpMessageHandler
            .When(HttpMethod.Post, "https://www.google.com/test.zip")
            .WithContent("Download")
            .Respond(HttpStatusCode.OK, new ByteArrayContent(bytes));

        var res = await _httpClient.SendAsync(new HttpRequest
        {
            Method = RequestMethod.Post,
            BodyText = "Download",
            Uri = new Uri("https://www.google.com/test.zip"),
        }, CancellationToken.None);

        res.MediaType.Should().BeNull();
        var textBody = await res.ReadAsStringAsync();
        textBody.Should().Be("Test Zip");
        var byteBody = await res.ReadAsByteArrayAsync();
        byteBody.Should().Equal(bytes);
    }

    [Theory]
    [InlineData("Content-Type", "application/json")]
    [InlineData("User-Agent", "Windows")]
    public async Task SendAsync_CustomHeader_PassThroughToServer(string name, string value)
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .WithHeaders(name, value)
            .Respond("text/plain", "Hello, Google!");

        await _httpClient.SendAsync(new HttpRequest
        {
            Uri = new Uri("https://www.google.com"),
            Headers = new Dictionary<string, string>
            {
                { name, value },
            },
        }, CancellationToken.None);

        _httpMessageHandler.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData("content-type")]
    [InlineData("Content-type")]
    [InlineData("CONTENT-TYPE")]
    public async Task SendAsync_SetReservedHeader_ThrowException(string name)
    {
        await _httpClient.Invoking(x =>
                x.SendAsync(new HttpRequest
                {
                    Uri = new Uri("https://www.google.com"),
                    BodyText = string.Empty,
                    Headers = new Dictionary<string, string>
                    {
                        { name, "application/json" },
                    },
                }, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"Misused header name, '{name}'*");
    }

    [Theory]
    [InlineData("X-Custom-Header", "CustomHeader-Value")]
    [InlineData("User-Agent", "dotnet-socketio[client]/socket")]
    [InlineData("user-agent", "dotnet-socketio[client]/socket")]
    public async Task SetDefaultHeader_CustomHeaderName_PassThroughToServer(string name, string value)
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .WithHeaders(name, value)
            .Respond("text/plain", "Hello, Google!");

        _httpClient.SetDefaultHeader(name, value);
        await _httpClient.SendAsync(new HttpRequest
        {
            Uri = new Uri("https://www.google.com"),
        }, CancellationToken.None);

        _httpMessageHandler.VerifyNoOutstandingExpectation();
    }
}