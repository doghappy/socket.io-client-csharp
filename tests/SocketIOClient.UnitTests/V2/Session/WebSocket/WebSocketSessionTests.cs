using System.Net.WebSockets;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Core;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.WebSocket;
using SocketIOClient.V2.UriConverter;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Session.WebSocket;

public class WebSocketSessionTests
{
    public WebSocketSessionTests(ITestOutputHelper output)
    {
        _wsAdapter = Substitute.For<IWebSocketAdapter>();
        _uriConverter = Substitute.For<IUriConverter>();
        _uriConverter.GetServerUri(true, Arg.Any<Uri>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<int>())
            .Returns(new Uri("ws://localhost:3000/socket.io"));
        var logger = output.CreateLogger<WebSocketSession>();
        _session = new WebSocketSession(
            logger,
            _wsAdapter,
            _uriConverter)
        {
            Options = _sessionOptions,
        };
    }

    private readonly SessionOptions _sessionOptions = new()
    {
        ServerUri = new Uri("http://localhost:3000"),
        Query = new List<KeyValuePair<string, string>>(),
        EngineIO = EngineIO.V4,
    };

    private readonly WebSocketSession _session;
    private readonly IWebSocketAdapter _wsAdapter;
    private readonly IUriConverter _uriConverter;


    #region ConnectAsync

    [Fact]
    public async Task ConnectAsync_AdapterThrowAnyException_SessionThrowConnectionFailedException()
    {
        _wsAdapter
            .ConnectAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Throws(new WebSocketException("Server refused connection"));

        await _session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionFailedException>()
            .WithMessage("Failed to connect to the server")
            .WithInnerException(typeof(WebSocketException))
            .WithMessage("Server refused connection");
    }

    [Fact]
    public async Task ConnectAsync_WhenCalled_PassCorrectArgsToAdapter()
    {
        await _session.ConnectAsync(CancellationToken.None);

        var expectedUri = new Uri("ws://localhost:3000/socket.io");
        await _wsAdapter.Received().ConnectAsync(expectedUri, CancellationToken.None);
    }

    [Fact]
    public async Task ConnectAsync_UriConverterThrow_PassThroughException()
    {
        _uriConverter.GetServerUri(Arg.Any<bool>(), Arg.Any<Uri>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<int>())
            .Throws(new Exception("UriConverter Error"));

        await _session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<Exception>()
            .WithMessage("UriConverter Error");
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_ThrowConnectionFailedException()
    {
        _wsAdapter
            .ConnectAsync(Arg.Any<Uri>(), Arg.Is<CancellationToken>(t => t.IsCancellationRequested))
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
    public async Task ConnectAsync_2ExtraHeaders_SetDefaultHeaderTwice()
    {
        _sessionOptions.ExtraHeaders = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        };

        await _session.ConnectAsync(CancellationToken.None);

        _wsAdapter.Received(1).SetDefaultHeader("key1", "value1");
        _wsAdapter.Received(1).SetDefaultHeader("key2", "value2");
    }

    [Fact]
    public async Task ConnectAsync_SetDefaultHeaderThrow_PassThroughException()
    {
        _wsAdapter
            .When(a => a.SetDefaultHeader(Arg.Any<string>(), Arg.Any<string>()))
            .Throws(new Exception("Unable to set header"));
        _sessionOptions.ExtraHeaders = new Dictionary<string, string>
        {
            { "key1", "value1" },
        };

        await _session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<Exception>()
            .WithMessage("Unable to set header");
    }

    #endregion
}