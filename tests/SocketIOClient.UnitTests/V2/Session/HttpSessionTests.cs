using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using SocketIOClient.V2.UriConverter;

namespace SocketIOClient.UnitTests.V2.Session;

public class HttpSessionTests
{
    public HttpSessionTests()
    {
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _engineIOAdapter = Substitute.For<IEngineIOAdapter>();
        _serializer = Substitute.For<ISerializer>();
        var sessionOptions = new SessionOptions
        {
            ServerUri = new Uri("http://localhost:3000"),
            Query = new List<KeyValuePair<string, string>>(),
        };
        _session = new HttpSession(
            sessionOptions,
            _engineIOAdapter,
            _httpAdapter,
            _serializer,
            new DefaultUriConverter(4));
    }

    private readonly HttpSession _session;
    private readonly IHttpAdapter _httpAdapter;
    private readonly IEngineIOAdapter _engineIOAdapter;
    private readonly ISerializer _serializer;

    [Fact]
    public async Task ConnectAsync_HttpAdapterThrowAnyException_SessionThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<IHttpRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Server refused connection"));

        await _session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionFailedException>()
            .WithMessage("Failed to connect to the server")
            .WithInnerException(typeof(HttpRequestException))
            .WithMessage("Server refused connection");
    }

    [Fact]
    public async Task ConnectAsync_WhenCalled_PassCorrectHttpRequestToAdapter()
    {
        var requests = new List<IHttpRequest>();
        _httpAdapter
            .When(async a => await a.SendAsync(Arg.Any<IHttpRequest>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => { requests.Add(callInfo.Arg<IHttpRequest>()); });

        await _session.ConnectAsync(CancellationToken.None);

        requests.Should()
            .BeEquivalentTo([
                new HttpRequest
                {
                    Uri = new Uri("http://localhost:3000/socket.io/?EIO=4&transport=polling"),
                    Method = RequestMethod.Get,
                },
            ]);
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_ThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<IHttpRequest>(), Arg.Is<CancellationToken>(t => t.IsCancellationRequested))
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
    public async Task Integration_HttpAdapterPushedMessages_MessagesWillBeForwardedToSubscribersOfHttpSession()
    {
        var httpClient = Substitute.For<IHttpClient>();
        var httpAdapter = new HttpAdapter(httpClient);
        var serializer = new SystemJsonSerializer(new Decapsulator());
        var uriConverter = new DefaultUriConverter(4);
        var sessionOptions = new SessionOptions
        {
            ServerUri = new Uri("http://localhost:3000"),
            Query = new List<KeyValuePair<string, string>>(),
        };
        var session = new HttpSession(sessionOptions, _engineIOAdapter, httpAdapter, serializer, uriConverter);
        var response = Substitute.For<IHttpResponse>();
        response.ReadAsStringAsync().Returns("any text");
        httpClient.SendAsync(Arg.Any<IHttpRequest>(), Arg.Any<CancellationToken>()).Returns(response);
        _engineIOAdapter.GetMessages(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "0{\"sid\":\"123\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                },
            ]);
        var observer = Substitute.For<IMyObserver<IMessage>>();
        session.Subscribe(observer);
        var captured = new List<IMessage>();
        observer
            .When(x => x.OnNextAsync(Arg.Any<IMessage>()))
            .Do(call => captured.Add(call.Arg<IMessage>()));

        await httpAdapter.SendAsync(new ProtocolMessage(), CancellationToken.None);

        captured.Should()
            .BeEquivalentTo(new List<IMessage>
            {
                new OpenedMessage
                {
                    Sid = "123",
                    Upgrades = ["websocket"],
                    PingInterval = 10000,
                    PingTimeout = 5000,
                },
            }, options => options.IncludingAllRuntimeProperties());
    }

    [Fact]
    public async Task OnNextAsync_BinaryMessageIsNotReady_NoMessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _session.Subscribe(observer);

        _serializer.Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonBinaryEventMessage
            {
                BytesCount = 1,
            });
        var protocolMessage = new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        };
        _engineIOAdapter.GetMessages(Arg.Any<string>()).Returns([protocolMessage]);

        await _session.OnNextAsync(protocolMessage);

        await observer.Received(0).OnNextAsync(Arg.Any<IMessage>());
        _session.PendingDeliveryCount.Should().Be(1);
    }

    [Fact]
    public async Task OnNextAsync_BinaryMessageReady_MessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _session.Subscribe(observer);

        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonBinaryEventMessage
            {
                BytesCount = 1,
            });

        _engineIOAdapter.GetMessages(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                },
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                    Bytes = [],
                },
            ]);
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await observer.Received(1).OnNextAsync(Arg.Any<IBinaryMessage>());
        _session.PendingDeliveryCount.Should().Be(0);
    }

    [Fact]
    public async Task OnNextAsync_BinaryAckMessageIsNotReady_NoMessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _session.Subscribe(observer);

        _serializer.Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonBinaryAckMessage
            {
                Id = 1,
                BytesCount = 1,
            });

        _engineIOAdapter.GetMessages(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                },
            ]);
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await observer.Received(0).OnNextAsync(Arg.Any<IMessage>());
        _session.PendingDeliveryCount.Should().Be(1);
    }

    [Fact]
    public async Task OnNextAsync_BinaryAckMessageReady_MessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _session.Subscribe(observer);

        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonBinaryAckMessage
            {
                Id = 1,
                BytesCount = 1,
            });

        _engineIOAdapter.GetMessages(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "EngineIOAdapter Messages",
                },
            ]);
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = [],
        });

        await observer.Received(1).OnNextAsync(Arg.Any<IBinaryMessage>());
        _session.PendingDeliveryCount.Should().Be(0);
    }

    [Fact]
    public async Task OnNextAsync_TextMessages_ProcessMessageAsyncOfEngineIOAdapterIsCalled()
    {
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new ConnectedMessage());

        _engineIOAdapter.GetMessages(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "EngineIOAdapter Messages",
                },
            ]);
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await _engineIOAdapter.Received(1).ProcessMessageAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task OnNextAsync_ByteMessages_ProcessMessageAsyncOfEngineIOAdapterIsNeverCalled()
    {
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonBinaryAckMessage
            {
                Id = 1,
                BytesCount = 1,
            });

        _engineIOAdapter.GetMessages(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "EngineIOAdapter Messages",
                },
            ]);
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await _engineIOAdapter.DidNotReceive().ProcessMessageAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task SendAsyncData_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        await _session.SendAsync([], CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsyncDataAndId_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>(), 12)
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        await _session.SendAsync([], 12, CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), Arg.Any<CancellationToken>());
    }
}