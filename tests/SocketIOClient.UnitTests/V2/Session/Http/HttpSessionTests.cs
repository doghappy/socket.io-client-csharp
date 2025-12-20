using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.Session.Http;
using SocketIOClient.V2.Session.Http.HttpEngineIOAdapter;
using SocketIOClient.V2.UriConverter;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Session.Http;

public class HttpSessionTests
{
    public HttpSessionTests(ITestOutputHelper output)
    {
        _httpAdapter = Substitute.For<IHttpAdapter>();
        var engineIOAdapterFactory = Substitute.For<IEngineIOAdapterFactory>();
        _engineIOAdapter = Substitute.For<IHttpEngineIOAdapter>();
        _engineIOAdapter.ToHttpRequest(Arg.Any<string>()).Returns(new HttpRequest());
        engineIOAdapterFactory
            .Create(Arg.Any<EngineIO>())
            .Returns(_engineIOAdapter);
        _serializer = Substitute.For<ISerializer>();
        var engineIOMessageAdapterFactory = Substitute.For<IEngineIOMessageAdapterFactory>();
        var logger = output.CreateLogger<HttpSession>();
        _uriConverter = Substitute.For<IUriConverter>();
        _uriConverter.GetServerUri(false, Arg.Any<Uri>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<int>())
            .Returns(new Uri("http://localhost:3000/socket.io/?EIO=4&transport=polling"));
        _session = new HttpSession(
            logger,
            engineIOAdapterFactory,
            _httpAdapter,
            _serializer,
            engineIOMessageAdapterFactory,
            _uriConverter)
        {
            Options = _sessionOptions
        };
    }

    private readonly SessionOptions _sessionOptions = new()
    {
        ServerUri = new Uri("http://localhost:3000"),
        Query = new List<KeyValuePair<string, string>>(),
        EngineIO = EngineIO.V4,
    };

    private readonly HttpSession _session;
    private readonly IHttpAdapter _httpAdapter;
    private readonly IHttpEngineIOAdapter _engineIOAdapter;
    private readonly ISerializer _serializer;
    private readonly IUriConverter _uriConverter;

    #region ConnectAsync

    [Fact]
    public async Task ConnectAsync_HttpAdapterThrowAnyException_SessionThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
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
        var requests = new List<HttpRequest>();
        _httpAdapter
            .When(async a => await a.SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => { requests.Add(callInfo.Arg<HttpRequest>()); });

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
        _httpAdapter
            .SendAsync(Arg.Any<HttpRequest>(), Arg.Is<CancellationToken>(t => t.IsCancellationRequested))
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
    public async Task ConnectAsync_WhenCalled_SetAdapterUri()
    {
        await _session.ConnectAsync(CancellationToken.None);
        _httpAdapter.Uri.Should().Be(new Uri("http://localhost:3000/socket.io/?EIO=4&transport=polling"));
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

        _httpAdapter.Received(1).SetDefaultHeader("key1", "value1");
        _httpAdapter.Received(1).SetDefaultHeader("key2", "value2");
    }

    [Fact]
    public async Task ConnectAsync_SetDefaultHeaderThrow_PassThroughException()
    {
        _httpAdapter
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

    [Fact]
    public async Task DisconnectAsync_NoNamespace_SendDisconnectToServer()
    {
        var request = new HttpRequest
        {
            BodyText = "2:41",
        };
        _engineIOAdapter.ToHttpRequest("41").Returns(request);

        await _session.DisconnectAsync(CancellationToken.None);

        await _httpAdapter.Received(1).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisconnectAsync_HasNamespace_SendDisconnectToServer()
    {
        _sessionOptions.Namespace = "/test";
        var request = new HttpRequest
        {
            BodyText = "41/test,",
        };
        _engineIOAdapter.ToHttpRequest("41/test,").Returns(request);

        await _session.DisconnectAsync(CancellationToken.None);

        await _httpAdapter.Received(1).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WhenCalled_HttpSessionIsSubscriberOfHttpAdapter()
    {
        _httpAdapter.Received(1).Subscribe(_session);
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
        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>()).Returns([protocolMessage]);

        await _session.OnNextAsync(protocolMessage);

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
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

        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
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

        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
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

        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "EngineIOAdapter Messages",
                },
            ]);
        _engineIOAdapter.ExtractMessagesFromBytes(Arg.Any<byte[]>())
            .Returns([
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

        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
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

        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
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
    public async Task OnNextAsync_ConnectedMessage_SetAdapterUriWithSid()
    {
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new OpenedMessage { Sid = "abc" });

        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "EngineIOAdapter Messages",
                },
            ]);
        _httpAdapter.Uri = new Uri("http://localhost:3000/socket.io/?EIO=3&transport=polling");

        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        _httpAdapter.Uri.Should().Be(new Uri("http://localhost:3000/socket.io/?EIO=3&transport=polling&sid=abc"));
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

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
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

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAckDataAsync_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.SerializeAckData(Arg.Any<object[]>(), 12)
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        await _session.SendAckDataAsync([], 12, CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_1TextMessage1ByteMessage_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage { Type = ProtocolMessageType.Text },
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
            ]);

        await _session.SendAsync([], CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_2ByteMessages_CallAdapter1Time()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
            ]);

        await _session.SendAsync([], CancellationToken.None);

        await _httpAdapter.Received(1).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Options_SetAnyValue_InitializationMethodsWereCalled()
    {
        _serializer.Received(1).SetEngineIOMessageAdapter(Arg.Any<IEngineIOMessageAdapter>());
        _engineIOAdapter.Received(1).Subscribe(Arg.Any<HttpSession>());
        _engineIOAdapter.Timeout.Should().Be(_sessionOptions.Timeout);
    }
}