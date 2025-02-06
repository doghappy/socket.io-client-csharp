using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.V2.Core;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;
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
        _session = new HttpSession(_engineIOAdapter, _httpAdapter, _serializer, new DefaultUriConverter())
        {
            SessionOptions = new SessionOptions
            {
                ServerUri = new Uri("http://localhost:3000"),
                EngineIO = EngineIO.V4,
                Query = new List<KeyValuePair<string, string>>(),
            },
        };
        // _httpAdapter.Subscribe(_session);
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

    [Fact]
    public async Task ConnectAsync_HttpAdapterThrowAnyException_SessionThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<ProtocolMessage>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Server refused connection"));

        await _session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionFailedException>()
            .WithMessage("Failed to connect to the server")
            .WithInnerException(typeof(HttpRequestException))
            .WithMessage("Server refused connection");
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_ThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<ProtocolMessage>(), Arg.Is<CancellationToken>(t => t.IsCancellationRequested))
            .Throws(new TaskCanceledException("Task was canceled"));

        await _session.Invoking(async x =>
            {
                using var cts = new CancellationTokenSource();
                await cts.CancelAsync();
                await x.ConnectAsync(cts.Token);
            })
            .Should()
            .ThrowExactlyAsync<ConnectionFailedException>()
            .WithMessage("Failed to connect to the server")
            .WithInnerException(typeof(TaskCanceledException))
            .WithMessage("Task was canceled");
    }

    [Fact]
    public async Task ConnectAsync_HttpAdapterWorks_OpenedMessageWillBePushed()
    {
        var response = Substitute.For<IHttpResponse>();
        response.ReadAsStringAsync().Returns("0{\"sid\":\"123\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}");
        var tuple = GetHttpSessionAdapterHttpClient();
        tuple.httpClient.SendAsync(Arg.Any<IHttpRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        await tuple.session.ConnectAsync(CancellationToken.None);
    }

    private (HttpSession session, HttpAdapter adapter, IHttpClient httpClient) GetHttpSessionAdapterHttpClient()
    {
        var httpClient = Substitute.For<IHttpClient>();
        var httpAdapter = new HttpAdapter(httpClient);
        var session = new HttpSession(_engineIOAdapter, httpAdapter, _serializer, new DefaultUriConverter())
        {
            SessionOptions = new SessionOptions
            {
                ServerUri = new Uri("http://localhost:3000"),
                EngineIO = EngineIO.V4,
                Query = new List<KeyValuePair<string, string>>(),
            },
        };
        return (session, httpAdapter, httpClient);
    }
}