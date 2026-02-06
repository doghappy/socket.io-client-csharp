using System.Collections.Specialized;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.SystemTextJson;
using SocketIOClient.Session;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.Http;
using SocketIOClient.Session.Http.EngineIOAdapter;
using SocketIOClient.Test.Core;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Session.Http;

public class HttpSessionTests
{
    public HttpSessionTests(ITestOutputHelper output)
    {
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _engineIOAdapterFactory = Substitute.For<IEngineIOAdapterFactory>();
        _engineIOAdapter = Substitute.For<IHttpEngineIOAdapter>();
        _engineIOAdapter.ToHttpRequest(Arg.Any<string>()).Returns(new HttpRequest());
        _engineIOAdapterFactory
            .Create<IHttpEngineIOAdapter>(Arg.Any<EngineIOCompatibility>())
            .Returns(_engineIOAdapter);
        _serializer = Substitute.For<ISerializer>();
        _engineIOMessageAdapterFactory = Substitute.For<IEngineIOMessageAdapterFactory>();
        _logger = output.CreateLogger<HttpSession>();
    }

    private HttpSession NewSession()
    {
        return new HttpSession(
            _logger,
            _engineIOAdapterFactory,
            _httpAdapter,
            _serializer,
            _engineIOMessageAdapterFactory)
        {
            Options = _sessionOptions
        };
    }

    private readonly SessionOptions _sessionOptions = new()
    {
        ServerUri = new Uri("http://localhost:3000"),
        Query = new NameValueCollection(),
        EngineIO = EngineIO.V4,
    };

    private readonly IHttpAdapter _httpAdapter;
    private readonly IHttpEngineIOAdapter _engineIOAdapter;
    private readonly ISerializer _serializer;
    private readonly IEngineIOAdapterFactory _engineIOAdapterFactory;
    private readonly ILogger<HttpSession> _logger;
    private readonly IEngineIOMessageAdapterFactory _engineIOMessageAdapterFactory;

    #region ConnectAsync

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_HttpAdapterThrowAnyException_ThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Server refused connection"));

        var session = NewSession();
        await session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionFailedException>()
            .WithMessage("Failed to connect to the server")
            .WithInnerException(typeof(HttpRequestException))
            .WithMessage("Server refused connection");
    }

    [Theory(Timeout = 5000)]
    [InlineData("http://localhost:3000", null, EngineIO.V3, "http://localhost:3000/socket.io/?EIO=3&transport=polling")]
    [InlineData("https://localhost:3000", null, EngineIO.V4, "https://localhost:3000/socket.io/?EIO=4&transport=polling")]
    [InlineData("http://localhost:3000", "", EngineIO.V3, "http://localhost:3000/socket.io/?EIO=3&transport=polling")]
    [InlineData("http://localhost:3000", "/app/", EngineIO.V4, "http://localhost:3000/app/?EIO=4&transport=polling")]
    [InlineData("http://example.com:3000", null, EngineIO.V4, "http://example.com:3000/socket.io/?EIO=4&transport=polling")]
    public async Task ConnectAsync_WhenCalled_PassCorrectHttpRequestToAdapter(string serverUri, string path, EngineIO eio, string expectedUri)
    {
        var requests = new List<HttpRequest>();
        _httpAdapter
            .When(async a => await a.SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => { requests.Add(callInfo.Arg<HttpRequest>()); });

        var session = NewSession();
        session.Options.ServerUri = new Uri(serverUri);
        session.Options.Path = path;
        session.Options.EngineIO = eio;
        await session.ConnectAsync(CancellationToken.None);

        requests.Should()
            .BeEquivalentTo([
                new HttpRequest
                {
                    Uri = new Uri(expectedUri),
                    Method = RequestMethod.Get,
                    IsConnect = true
                },
            ]);
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_CancelledToken_ThrowConnectionFailedException()
    {
        _httpAdapter
            .SendAsync(Arg.Any<HttpRequest>(), Arg.Is<CancellationToken>(t => t.IsCancellationRequested))
            .Throws(new TaskCanceledException("Task was canceled"));

        var session = NewSession();
        await session.Invoking(async x =>
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

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_WhenCalled_SetAdapterUri()
    {
        var session = NewSession();
        await session.ConnectAsync(CancellationToken.None);
        _httpAdapter.Uri.Should().Be(new Uri("http://localhost:3000/socket.io/?EIO=4&transport=polling"));
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_EvenSidIsNotNull_SidIsIgnored()
    {
        var session = NewSession();
        session.Options.Sid = "123456";

        await session.ConnectAsync(CancellationToken.None);

        var expectedUri = new Uri("http://localhost:3000/socket.io/?EIO=4&transport=polling");
        await _httpAdapter.Received()
            .SendAsync(Arg.Is<HttpRequest>(r => r.Uri == expectedUri), Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_OptionQueryHasAnyItems_AppendOptionQueryToUrl()
    {
        var session = NewSession();
        session.Options.Query = new NameValueCollection
        {
            { "test", "123" },
            { "test", "456" },
            { "🐮", "🍺" },
            { "你", "好" }
        };

        await session.ConnectAsync(CancellationToken.None);

        var expectedUri = new Uri("http://localhost:3000/socket.io/?EIO=4&transport=polling&test=123%2C456&%F0%9F%90%AE=%F0%9F%8D%BA&%E4%BD%A0=%E5%A5%BD");
        await _httpAdapter.Received()
            .SendAsync(Arg.Is<HttpRequest>(r => r.Uri == expectedUri), Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_2ExtraHeaders_SetDefaultHeaderTwice()
    {
        _sessionOptions.ExtraHeaders = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        };

        var session = NewSession();
        await session.ConnectAsync(CancellationToken.None);

        _httpAdapter.Received(1).SetDefaultHeader("key1", "value1");
        _httpAdapter.Received(1).SetDefaultHeader("key2", "value2");
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_SetDefaultHeaderThrow_PassThroughException()
    {
        _httpAdapter
            .When(a => a.SetDefaultHeader(Arg.Any<string>(), Arg.Any<string>()))
            .Throws(new Exception("Unable to set header"));
        _sessionOptions.ExtraHeaders = new Dictionary<string, string>
        {
            { "key1", "value1" },
        };

        var session = NewSession();
        await session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<Exception>()
            .WithMessage("Unable to set header");
    }

    [Fact]
    public void OnDisconnected_AdapterIsInvoked_SessionShouldBeInvoked()
    {
        var session = NewSession();
        session.OnDisconnected = Substitute.For<Action>();

        _httpAdapter.OnDisconnected();

        session.OnDisconnected.Received().Invoke();
    }

    #endregion

    [Fact(Timeout = 5000)]
    public async Task DisconnectAsync_NoNamespace_SendDisconnectToServer()
    {
        var request = new HttpRequest
        {
            BodyText = "2:41",
        };
        _engineIOAdapter.ToHttpRequest("41").Returns(request);

        var session = NewSession();
        await session.DisconnectAsync(CancellationToken.None);

        await _httpAdapter.Received(1).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task DisconnectAsync_HasNamespace_SendDisconnectToServer()
    {
        _sessionOptions.Namespace = "/test";
        var request = new HttpRequest
        {
            BodyText = "41/test,",
        };
        _engineIOAdapter.ToHttpRequest("41/test,").Returns(request);

        var session = NewSession();
        await session.DisconnectAsync(CancellationToken.None);

        await _httpAdapter.Received(1).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WhenCalled_HttpSessionIsSubscriberOfHttpAdapter()
    {
        var session = NewSession();
        _httpAdapter.Received(1).Subscribe(session);
    }

    [Fact(Timeout = 5000)]
    public async Task OnNextAsync_BinaryMessageIsNotReady_NoMessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        var session = NewSession();
        session.Subscribe(observer);

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

        await session.OnNextAsync(protocolMessage);

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
        session.PendingDeliveryCount.Should().Be(1);
    }

    [Fact(Timeout = 5000)]
    public async Task OnNextAsync_BinaryMessageReady_MessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        var session = NewSession();
        session.Subscribe(observer);

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
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await observer.Received(1).OnNextAsync(Arg.Any<IBinaryMessage>());
        session.PendingDeliveryCount.Should().Be(0);
    }

    [Fact(Timeout = 5000)]
    public async Task OnNextAsync_BinaryAckMessageIsNotReady_NoMessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        var session = NewSession();
        session.Subscribe(observer);

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
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await observer.Received(0).OnNextAsync(Arg.Any<IMessage>());
        session.PendingDeliveryCount.Should().Be(1);
    }

    [Fact(Timeout = 5000)]
    public async Task OnNextAsync_BinaryAckMessageReady_MessageWillBePushed()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        var session = NewSession();
        session.Subscribe(observer);

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
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = [],
        });

        await observer.Received(1).OnNextAsync(Arg.Any<IBinaryMessage>());
        session.PendingDeliveryCount.Should().Be(0);
    }

    [Fact(Timeout = 5000)]
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
        var session = NewSession();
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await _engineIOAdapter.Received(1).ProcessMessageAsync(Arg.Any<IMessage>());
    }

    [Fact(Timeout = 5000)]
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
        var session = NewSession();
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await _engineIOAdapter.DidNotReceive().ProcessMessageAsync(Arg.Any<IMessage>());
    }

    [Fact(Timeout = 5000)]
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

        var session = NewSession();
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        _httpAdapter.Uri.Should().Be(new Uri("http://localhost:3000/socket.io/?EIO=3&transport=polling&sid=abc"));
    }

    [Fact(Timeout = 5000)]
    public async Task OnNextAsync_OpenedMessageAndProcessMessageAsyncThrows_AdapterUriIsUpdated()
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
        _engineIOAdapter.ProcessMessageAsync(Arg.Any<IMessage>()).ThrowsAsync(new Exception("Test"));

        var session = NewSession();
        await session.Invoking(s => s.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        }))
        .Should()
        .ThrowAsync<Exception>()
        .WithMessage("Test");

        _httpAdapter.Uri.Should().Be(new Uri("http://localhost:3000/socket.io/?EIO=3&transport=polling&sid=abc"));
    }

    [Fact(Timeout = 5000)]
    public async Task OnNextAsync_MessageIsProcessed_NotSendToObserver()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        var session = NewSession();
        session.Subscribe(observer);
        _engineIOAdapter.ProcessMessageAsync(Arg.Any<IMessage>()).Returns(true);
        _engineIOAdapter.ExtractMessagesFromText(Arg.Any<string>())
            .Returns([
                new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = "EngineIOAdapter Messages",
                },
            ]);
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new OpenedMessage { Sid = "abc" });
        _httpAdapter.Uri = new Uri("http://localhost:3000/socket.io/?EIO=3&transport=polling");

        await session.OnNextAsync(new ProtocolMessage());

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
    }

    [Fact(Timeout = 5000)]
    public async Task SendAsyncData_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        var session = NewSession();
        await session.SendAsync([], CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task SendAsyncDataAndId_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>(), 12)
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        var session = NewSession();
        await session.SendAsync([], 12, CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task SendAckDataAsync_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.SerializeAckData(Arg.Any<object[]>(), 12)
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        var session = NewSession();
        await session.SendAckDataAsync([], 12, CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task SendAsync_1TextMessage1ByteMessage_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage { Type = ProtocolMessageType.Text },
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
            ]);

        var session = NewSession();
        await session.SendAsync([], CancellationToken.None);

        await _httpAdapter.Received(2).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact(Timeout = 5000)]
    public async Task SendAsync_2ByteMessages_CallAdapter1Time()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
            ]);

        var session = NewSession();
        await session.SendAsync([], CancellationToken.None);

        await _httpAdapter.Received(1).SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Options_SetAnyValue_InitializationMethodsWereCalled()
    {
        _sessionOptions.Timeout = TimeSpan.FromSeconds(1);
        _sessionOptions.Namespace = "/test";
        _sessionOptions.Auth = new { user = "admin", password = "123456" };
        _sessionOptions.AutoUpgrade = true;

        NewSession();

        _serializer.Received(1).SetEngineIOMessageAdapter(Arg.Any<IEngineIOMessageAdapter>());
        _engineIOAdapter.Received(1).Subscribe(Arg.Any<HttpSession>());
        _engineIOAdapter.Options.Timeout.Should().Be(TimeSpan.FromSeconds(1));
        _engineIOAdapter.Options.Namespace.Should().Be("/test");
        _engineIOAdapter.Options.Auth.Should().BeEquivalentTo(new { user = "admin", password = "123456" });
        _engineIOAdapter.Options.AutoUpgrade.Should().BeTrue();
        _serializer.Namespace.Should().Be("/test");
    }

    [Theory]
    [InlineData(EngineIO.V3, EngineIOCompatibility.HttpEngineIO3)]
    [InlineData(EngineIO.V4, EngineIOCompatibility.HttpEngineIO4)]
    public void Options_DifferentEngineIOVersion_SelectRelatedCompatibility(
        EngineIO engineIO, EngineIOCompatibility expectedCompatibility)
    {
        _sessionOptions.EngineIO = engineIO;
        NewSession();
        _engineIOAdapterFactory.Received(1).Create<IHttpEngineIOAdapter>(expectedCompatibility);
    }
}