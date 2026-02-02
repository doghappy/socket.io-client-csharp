using System.Collections.Specialized;
using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.WebSocket;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.SystemTextJson;
using SocketIOClient.Session;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.WebSocket;
using SocketIOClient.Session.WebSocket.EngineIOAdapter;
using SocketIOClient.Test.Core;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Session.WebSocket;

public class WebSocketSessionTests
{
    public WebSocketSessionTests(ITestOutputHelper output)
    {
        _wsAdapter = Substitute.For<IWebSocketAdapter>();
        _engineIOAdapterFactory = Substitute.For<IEngineIOAdapterFactory>();
        _engineIOAdapter = Substitute.For<IWebSocketEngineIOAdapter>();
        _engineIOAdapterFactory
            .Create<IWebSocketEngineIOAdapter>(Arg.Any<EngineIOCompatibility>())
            .Returns(_engineIOAdapter);
        _serializer = Substitute.For<ISerializer>();
        _engineIOMessageAdapterFactory = Substitute.For<IEngineIOMessageAdapterFactory>();
        _logger = output.CreateLogger<WebSocketSession>();
    }

    private WebSocketSession NewSession()
    {
        return new WebSocketSession(
            _logger,
            _engineIOAdapterFactory,
            _wsAdapter,
            _serializer,
            _engineIOMessageAdapterFactory)
        {
            Options = _sessionOptions,
        };
    }

    private readonly SessionOptions _sessionOptions = new()
    {
        ServerUri = new Uri("http://localhost:3000"),
        Query = new NameValueCollection(),
        EngineIO = EngineIO.V4,
    };

    private readonly IWebSocketAdapter _wsAdapter;
    private readonly ISerializer _serializer;
    private readonly IWebSocketEngineIOAdapter _engineIOAdapter;
    private readonly IEngineIOAdapterFactory _engineIOAdapterFactory;
    private readonly ILogger<WebSocketSession> _logger;
    private readonly IEngineIOMessageAdapterFactory _engineIOMessageAdapterFactory;

    #region ConnectAsync

    [Fact]
    public async Task ConnectAsync_AdapterThrowAnyException_ThrowConnectionFailedException()
    {
        _wsAdapter
            .ConnectAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Throws(new WebSocketException("Server refused connection"));

        var session = NewSession();
        await session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionFailedException>()
            .WithMessage("Failed to connect to the server")
            .WithInnerException(typeof(WebSocketException))
            .WithMessage("Server refused connection");
    }

    [Theory]
    [InlineData("http://localhost:3000", null, EngineIO.V3, "ws://localhost:3000/socket.io/?EIO=3&transport=websocket")]
    [InlineData("https://localhost:3000", null, EngineIO.V4, "wss://localhost:3000/socket.io/?EIO=4&transport=websocket")]
    [InlineData("http://localhost:3000", "", EngineIO.V3, "ws://localhost:3000/socket.io/?EIO=3&transport=websocket")]
    [InlineData("http://localhost:3000", "/app/", EngineIO.V4, "ws://localhost:3000/app/?EIO=4&transport=websocket")]
    [InlineData("https://example.com:3000", null, EngineIO.V4, "wss://example.com:3000/socket.io/?EIO=4&transport=websocket")]
    public async Task ConnectAsync_WhenCalled_PassCorrectArgsToAdapter(string serverUri, string path, EngineIO eio, string expectedUri)
    {
        var session = NewSession();
        session.Options.ServerUri = new Uri(serverUri);
        session.Options.Path = path;
        session.Options.EngineIO = eio;

        await session.ConnectAsync(CancellationToken.None);

        await _wsAdapter.Received().ConnectAsync(new Uri(expectedUri), CancellationToken.None);
    }

    [Fact]
    public async Task ConnectAsync_SidIsNotNull_SendUpgradeMessageToServer()
    {
        var session = NewSession();
        session.Options.Sid = "123456";

        await session.ConnectAsync(CancellationToken.None);

        var expectedUri = new Uri("ws://localhost:3000/socket.io/?EIO=4&transport=websocket&sid=123456");
        await _wsAdapter.Received().ConnectAsync(expectedUri, CancellationToken.None);
        await _wsAdapter.Received()
            .SendAsync(Arg.Is<ProtocolMessage>(m => m.Text == "5"), CancellationToken.None);
    }

    [Fact]
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

        var expectedUri = new Uri("ws://localhost:3000/socket.io/?EIO=4&transport=websocket&test=123%2C456&%F0%9F%90%AE=%F0%9F%8D%BA&%E4%BD%A0=%E5%A5%BD");
        await _wsAdapter.Received().ConnectAsync(expectedUri, CancellationToken.None);
    }

    [Fact]
    public async Task ConnectAsync_CancelledToken_ThrowConnectionFailedException()
    {
        _wsAdapter
            .ConnectAsync(Arg.Any<Uri>(), Arg.Is<CancellationToken>(t => t.IsCancellationRequested))
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

    [Fact]
    public async Task ConnectAsync_2ExtraHeaders_SetDefaultHeaderTwice()
    {
        _sessionOptions.ExtraHeaders = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        };

        var session = NewSession();
        await session.ConnectAsync(CancellationToken.None);

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

        var session = NewSession();
        await session.Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<Exception>()
            .WithMessage("Unable to set header");
    }

    #endregion

    [Fact]
    public async Task DisconnectAsync_NoNamespace_SendDisconnectToServer()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var session = NewSession();
        await session.DisconnectAsync(token);

        await _wsAdapter.Received(1)
            .SendAsync(Arg.Is<ProtocolMessage>(m =>
                m.Type == ProtocolMessageType.Text && m.Text == "41"), token);
    }

    [Fact]
    public async Task DisconnectAsync_HasNamespace_SendDisconnectToServer()
    {
        _sessionOptions.Namespace = "/test";
        var session = NewSession();
        await session.DisconnectAsync(CancellationToken.None);

        await _wsAdapter.Received(1)
            .SendAsync(Arg.Is<ProtocolMessage>(m =>
                m.Type == ProtocolMessageType.Text && m.Text == "41/test,"), CancellationToken.None);
    }

    [Fact]
    public void Constructor_WhenCalled_WsSessionIsSubscriberOfWsAdapter()
    {
        var session = NewSession();
        _wsAdapter.Received(1).Subscribe(session);
    }

    [Fact]
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

        await session.OnNextAsync(protocolMessage);

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
        session.PendingDeliveryCount.Should().Be(1);
    }

    [Fact]
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

        await session.OnNextAsync(new ProtocolMessage());
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = [],
        });

        await observer.Received(1).OnNextAsync(Arg.Any<IBinaryMessage>());
        session.PendingDeliveryCount.Should().Be(0);
    }

    [Fact]
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

        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
        session.PendingDeliveryCount.Should().Be(1);
    }

    [Fact]
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

    [Fact]
    public async Task OnNextAsync_TextMessages_ProcessMessageAsyncOfEngineIOAdapterIsCalled()
    {
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new ConnectedMessage());

        var session = NewSession();
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await _engineIOAdapter.Received(1).ProcessMessageAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task OnNextAsync_MessageIsProcessed_NotSendToObserver()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        var session = NewSession();
        session.Subscribe(observer);
        _engineIOAdapter.ProcessMessageAsync(Arg.Any<IMessage>()).Returns(true);
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new OpenedMessage { Sid = "abc" });

        await session.OnNextAsync(new ProtocolMessage());

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task SendAsyncData_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        var session = NewSession();
        await session.SendAsync([], CancellationToken.None);

        await _wsAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task SendAsyncDataAndId_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>(), 12)
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        var session = NewSession();
        await session.SendAsync([], 12, CancellationToken.None);

        await _wsAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task SendAckDataAsync_SerializerReturn2Messages_CallAdapter2Times()
    {
        _serializer.SerializeAckData(Arg.Any<object[]>(), 12)
            .Returns([
                new ProtocolMessage(),
                new ProtocolMessage(),
            ]);

        var session = NewSession();
        await session.SendAckDataAsync([], 12, CancellationToken.None);

        await _wsAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_1TextMessage1ByteMessage_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage { Type = ProtocolMessageType.Text },
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
            ]);

        var session = NewSession();
        await session.SendAsync([], CancellationToken.None);

        await _wsAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_2ByteMessages_CallAdapter2Times()
    {
        _serializer.Serialize(Arg.Any<object[]>())
            .Returns([
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
                new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [] },
            ]);

        var session = NewSession();
        await session.SendAsync([], CancellationToken.None);

        await _wsAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), CancellationToken.None);
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
        _engineIOAdapter.Received(1).Subscribe(Arg.Any<WebSocketSession>());
        _engineIOAdapter.Options.Timeout.Should().Be(TimeSpan.FromSeconds(1));
        _engineIOAdapter.Options.Namespace.Should().Be("/test");
        _engineIOAdapter.Options.Auth.Should().BeEquivalentTo(new { user = "admin", password = "123456" });
        _engineIOAdapter.Options.AutoUpgrade.Should().BeTrue();
        _serializer.Namespace.Should().Be("/test");
    }

    [Theory]
    [InlineData(EngineIO.V3, EngineIOCompatibility.WebSocketEngineIO3)]
    [InlineData(EngineIO.V4, EngineIOCompatibility.WebSocketEngineIO4)]
    public void Options_DifferentEngineIOVersion_SelectRelatedCompatibility(
        EngineIO engineIO, EngineIOCompatibility expectedCompatibility)
    {
        _sessionOptions.EngineIO = engineIO;
        NewSession();
        _engineIOAdapterFactory.Received(1).Create<IWebSocketEngineIOAdapter>(expectedCompatibility);
    }

    [Fact]
    public async Task OnNextAsync_TextMessage_WriteProtocolFrameNeverCalled()
    {
        var session = NewSession();
        _serializer.Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonEventMessage());
        var protocolMessage = new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        };

        await session.OnNextAsync(protocolMessage);

        _engineIOAdapter.DidNotReceive().ReadProtocolFrame(Arg.Any<byte[]>());
    }

    [Fact]
    public async Task OnNextAsync_BytesMessage_WriteProtocolFrameAlwaysCalled()
    {
        var session = NewSession();
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new SystemJsonBinaryAckMessage
            {
                Id = 1,
                BytesCount = 1,
            });

        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });
        await session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = []
        });

        _engineIOAdapter.Received().ReadProtocolFrame(Arg.Any<byte[]>());
    }
}