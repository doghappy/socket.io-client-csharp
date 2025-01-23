using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RichardSzalay.MockHttp;
using SocketIOClient.V2.Protocol.Http;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Protocol.Http;

public class SystemHttpClientTests
{
    public SystemHttpClientTests()
    {
        _httpMessageHandler = new MockHttpMessageHandler();
        _httpClient = new SystemHttpClient(_httpMessageHandler.ToHttpClient());
    }

    private readonly SystemHttpClient _httpClient;
    private readonly MockHttpMessageHandler _httpMessageHandler;

    [Fact]
    public async Task SendAsync_WhenReturnStringContent_AlwaysPass()
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .Respond("text/plain", "Hello, Google!");

        var res = await _httpClient.SendAsync(new HttpRequest
        {
            Uri = new Uri("https://www.google.com"),
        });

        res.MediaType.Should().Be("text/plain");
        var body = await res.ReadAsStringAsync();
        body.Should().Be("Hello, Google!");
    }

    [Fact]
    public async Task SendAsync_WhenTimeout_ThrowTaskCanceledException()
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .Respond("text/plain", "Hello, Google!");

        _httpClient.Timeout = TimeSpan.Zero;
        await _httpClient.Invoking(async x => await x.SendAsync(new HttpRequest
            {
                Uri = new Uri("https://www.google.com"),
            }))
            .Should()
            .ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task SendAsync_WhenPassACanceledToken_ThrowTaskCanceledException()
    {
        _httpMessageHandler
            .When("https://www.google.com")
            .Respond("text/plain", "Hello, Google!");

        _httpClient.Timeout = TimeSpan.Zero;
        using var cts = new CancellationTokenSource();
        cts.Cancel(true);
        var token = cts.Token;

        await _httpClient.Invoking(async x => await x.SendAsync(new HttpRequest
            {
                Uri = new Uri("https://www.google.com"),
            }, token))
            .Should()
            .ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task SendAsync_WhenResponseByteArrayContent_AlwaysPass()
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
        });

        res.MediaType.Should().BeNull();
        var textBody = await res.ReadAsStringAsync();
        textBody.Should().Be("Test Zip");
        var byteBody = await res.ReadAsByteArrayAsync();
        byteBody.Should().Equal(bytes);
    }
}