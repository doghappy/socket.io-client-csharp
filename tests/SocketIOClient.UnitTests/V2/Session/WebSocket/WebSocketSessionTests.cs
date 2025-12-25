using System.Net.WebSockets;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.WebSocket;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using SocketIOClient.V2.Session.EngineIOAdapter;
using SocketIOClient.V2.Session.WebSocket;
using SocketIOClient.V2.UriConverter;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Session.WebSocket;

public class WebSocketSessionTests
{
    public WebSocketSessionTests(ITestOutputHelper output)
    {
        _wsAdapter = Substitute.For<IWebSocketAdapter>();
        var engineIOAdapterFactory = Substitute.For<IEngineIOAdapterFactory>();
        _engineIOAdapter = Substitute.For<IEngineIOAdapter>();
        engineIOAdapterFactory
            .Create(Arg.Any<EngineIO>())
            .Returns(_engineIOAdapter);
        _serializer = Substitute.For<ISerializer>();
        var engineIOMessageAdapterFactory = Substitute.For<IEngineIOMessageAdapterFactory>();
        _uriConverter = Substitute.For<IUriConverter>();
        _uriConverter.GetServerUri(true, Arg.Any<Uri>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<KeyValuePair<string, string>>>(), Arg.Any<int>())
            .Returns(new Uri("ws://localhost:3000/socket.io"));
        var logger = output.CreateLogger<WebSocketSession>();
        _session = new WebSocketSession(
            logger,
            engineIOAdapterFactory,
            _wsAdapter,
            _serializer,
            engineIOMessageAdapterFactory,
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
    private readonly ISerializer _serializer;
    private readonly IUriConverter _uriConverter;
    private readonly IEngineIOAdapter _engineIOAdapter;

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

    [Fact]
    public async Task DisconnectAsync_NoNamespace_SendDisconnectToServer()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await _session.DisconnectAsync(token);

        await _wsAdapter.Received(1)
            .SendAsync(Arg.Is<ProtocolMessage>(m =>
                m.Type == ProtocolMessageType.Text && m.Text == "41"), token);
    }

    [Fact]
    public async Task DisconnectAsync_HasNamespace_SendDisconnectToServer()
    {
        _sessionOptions.Namespace = "/test";
        await _session.DisconnectAsync(CancellationToken.None);

        await _wsAdapter.Received(1)
            .SendAsync(Arg.Is<ProtocolMessage>(m =>
                m.Type == ProtocolMessageType.Text && m.Text == "41/test,"), CancellationToken.None);
    }

    [Fact]
    public void Constructor_WhenCalled_WsSessionIsSubscriberOfWsAdapter()
    {
        _wsAdapter.Received(1).Subscribe(_session);
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

        await _session.OnNextAsync(new ProtocolMessage());
        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = [],
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

        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await observer.DidNotReceive().OnNextAsync(Arg.Any<IMessage>());
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

        await _session.OnNextAsync(new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
        });

        await _engineIOAdapter.Received(1).ProcessMessageAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task OnNextAsync_MessageIsProcessed_NotSendToObserver()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _session.Subscribe(observer);
        _engineIOAdapter.ProcessMessageAsync(Arg.Any<IMessage>()).Returns(true);
        _serializer
            .Deserialize(Arg.Any<string>())
            .Returns(new OpenedMessage { Sid = "abc" });

        await _session.OnNextAsync(new ProtocolMessage());

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

        await _session.SendAsync([], CancellationToken.None);

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

        await _session.SendAsync([], 12, CancellationToken.None);

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

        await _session.SendAckDataAsync([], 12, CancellationToken.None);

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

        await _session.SendAsync([], CancellationToken.None);

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

        await _session.SendAsync([], CancellationToken.None);

        await _wsAdapter.Received(2).SendAsync(Arg.Any<ProtocolMessage>(), CancellationToken.None);
    }

    [Fact]
    public void Options_SetAnyValue_InitializationMethodsWereCalled()
    {
        _serializer.Received(1).SetEngineIOMessageAdapter(Arg.Any<IEngineIOMessageAdapter>());
        _engineIOAdapter.Received(1).Subscribe(Arg.Any<WebSocketSession>());
        _engineIOAdapter.Options.Timeout.Should().Be(_sessionOptions.Timeout);
        _engineIOAdapter.Options.Namespace.Should().Be(_sessionOptions.Namespace);
        _engineIOAdapter.Options.Auth.Should().Be(_sessionOptions.Auth);
    }
}