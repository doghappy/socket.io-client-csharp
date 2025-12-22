using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.Http.HttpEngineIOAdapter;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2.Session.Http.HttpEngineIOAdapter;

public class HttpEngineIO3AdapterTests
{
    public HttpEngineIO3AdapterTests(ITestOutputHelper output)
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _httpAdapter.IsReadyToSend.Returns(true);
        _retryPolicy = Substitute.For<IRetriable>();
        _retryPolicy.RetryAsync(2, Arg.Any<Func<Task>>()).Returns(async _ =>
        {
            await Task.Delay(50);
        });
        _logger = output.CreateLogger<HttpEngineIO3Adapter>();
        _adapter = new(
            _stopwatch,
            _httpAdapter,
            _retryPolicy,
            _logger);
    }

    private readonly IStopwatch _stopwatch;
    private readonly IHttpAdapter _httpAdapter;
    private readonly HttpEngineIO3Adapter _adapter;
    private readonly IRetriable _retryPolicy;
    private readonly ILogger<HttpEngineIO3Adapter> _logger;

    [Fact]
    public void ToHttpRequest_GivenAnEmptyArray_ThrowException()
    {
        _adapter
            .Invoking(x => x.ToHttpRequest(new List<byte[]>()))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The array cannot be empty");
    }

    private static readonly (ICollection<byte[]> bytes, HttpRequest req) OneItem1Byte = new(new List<byte[]>
    {
        new byte[] { 1 },
    }, new HttpRequest
    {
        BodyBytes = [1, 2, 255, 4, 1],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static readonly (ICollection<byte[]> bytes, HttpRequest req) OneItem10Bytes = new(new List<byte[]>
    {
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyBytes = [1, 1, 1, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static readonly (ICollection<byte[]> bytes, HttpRequest req) TwoItems1Byte10Bytes = new(new List<byte[]>
    {
        new byte[] { 1 },
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyBytes = [1, 2, 255, 4, 1, 1, 1, 1, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static IEnumerable<(ICollection<byte[]> bytes, HttpRequest req)> ToHttpRequestStrongTypeCases
    {
        get
        {
            yield return OneItem1Byte;
            yield return OneItem10Bytes;
            yield return TwoItems1Byte10Bytes;
        }
    }

    public static IEnumerable<object[]> ToHttpRequestCases =>
        ToHttpRequestStrongTypeCases.Select(x => new object[] { x.bytes, x.req });

    [Theory]
    [MemberData(nameof(ToHttpRequestCases))]
    public void ToHttpRequest_WhenCalled_AlwaysPass(ICollection<byte[]> bytes, HttpRequest result)
    {
        var req = _adapter.ToHttpRequest(bytes);
        req.Should().BeEquivalentTo(result);
    }

    private static readonly (string raw, IEnumerable<string> textMessages) GetMessagesSingleHelloWorld = new(
        "12:hello world!",
        ["hello world!"]);

    private static readonly (string raw, IEnumerable<string> textMessages) GetMessagesPingAndHelloWorld = new(
        "1:212:hello world!",
        ["2", "hello world!"]);

    public static TheoryData<string, IEnumerable<string>> GetMessagesCases =>
        new()
        {
            {
                GetMessagesSingleHelloWorld.raw,
                GetMessagesSingleHelloWorld.textMessages
            },
            {
                GetMessagesPingAndHelloWorld.raw,
                GetMessagesPingAndHelloWorld.textMessages
            },
        };

    [Theory]
    [MemberData(nameof(GetMessagesCases))]
    public void GetMessages_WhenCalled_AlwaysPass(string raw, IEnumerable<string> textMessages)
    {
        // Note: EngineIO3 bytes are handled by HttpSession directly, no need to test bytes here
        _adapter.ExtractMessagesFromText(raw)
            .Select(m => m.Text)
            .Should()
            .BeEquivalentTo(textMessages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToHttpRequest_GivenAnInvalidContent_ThrowException(string? content)
    {
        _adapter
            .Invoking(x => x.ToHttpRequest(content))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The content cannot be null or empty");
    }

    [Theory]
    [InlineData(" ", "1: ")]
    [InlineData("hello, world!", "13:hello, world!")]
    public void ToHttpRequest_GivenValidContent_ReturnLengthFollowedByItself(string content, string expected)
    {
        var req = _adapter.ToHttpRequest(content);
        req.BodyText.Should().Be(expected);
    }

    [Fact]
    public async Task ProcessMessageAsync_ConnectedMessage_PingInBackground()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        var range = Quantity.Within(6, 11);
        await _retryPolicy.Received(range).RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task ProcessMessageAsync_OpenedMessage_PollingInBackground()
    {
        _retryPolicy.RetryAsync(2, Arg.Any<Func<Task>>())
            .Returns(async _ => await Task.Delay(10));

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 10 });

        await Task.Delay(100);

        var range = Quantity.Within(5, 15);
        await _retryPolicy.Received(range).RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task ProcessMessageAsync_PongMessage_OnlySetDurationNotNotifyToSubscriber()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);
        _stopwatch.Elapsed.Returns(TimeSpan.FromSeconds(1));

        var pong = new PongMessage();
        await _adapter.ProcessMessageAsync(pong);

        await observer
            .DidNotReceive()
            .OnNextAsync(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task StartPingAsync_ObserverThrowException_ContinuePing()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        observer.OnNextAsync(Arg.Any<IMessage>()).ThrowsAsync(new Exception());
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        var range = Quantity.Within(5, 12);
        await _retryPolicy.Received().RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task StartPingAsync_WhenCalled_FirstDelayThenPing()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 100,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(50);

        await _retryPolicy.DidNotReceive().RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .DidNotReceive()
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task StartPingAsync_IsNotReadyToSend_DidNotPing()
    {
        var httpAdapter = Substitute.For<IHttpAdapter>();
        var adapter = new HttpEngineIO3Adapter(
            _stopwatch,
            httpAdapter,
            _retryPolicy,
            _logger);

        await adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 10 });
        await adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task ProcessMessageAsync_IsReadyAfter30ms_PingIsWorking()
    {
        _httpAdapter.IsReadyToSend.Returns(false);

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());
        _ = Task.Run(async () =>
        {
            await Task.Delay(30);
            _httpAdapter.IsReadyToSend.Returns(true);
        });

        await Task.Delay(200);

        await _retryPolicy.Received().RetryAsync(3, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task StartPingAsync_DisposeIsCalled_NeverPing()
    {
        _adapter.Dispose();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(200);

        await _retryPolicy.DidNotReceive().RetryAsync(3, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ConnectedMessageButNoOpenedMessage_ThrowException()
    {
        var connectedMessage = new ConnectedMessage();
        await _adapter.Invoking(x => x.ProcessMessageAsync(connectedMessage))
            .Should()
            .ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task ProcessMessageAsync_OpenedMessageThenConnectedMessage_SidIsNotNull()
    {
        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            Sid = "123",
        });
        var connectedMessage = new ConnectedMessage();
        await _adapter.ProcessMessageAsync(connectedMessage);
        connectedMessage.Sid.Should().Be("123");
    }

    [Fact]
    public async Task PollingAsync_HttpRequestExceptionOccurred_DoNotContinue()
    {
        _retryPolicy.RetryAsync(2, Arg.Any<Func<Task>>())
            .Returns(_ => Task.FromException(new HttpRequestException()));

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });

        await Task.Delay(100);

        await _retryPolicy
            .Received(1)
            .RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task ProcessMessageAsync_IsReadyAfter30ms_PollingIsWorking()
    {
        _httpAdapter.IsReadyToSend.Returns(false);

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        _ = Task.Run(async () =>
        {
            await Task.Delay(30);
            _httpAdapter.IsReadyToSend.Returns(true);
        });

        await Task.Delay(200);

        await _retryPolicy.Received().RetryAsync(2, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task PollingAsync_DisposeIsCalled_NeverPolling()
    {
        _adapter.Dispose();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });

        await Task.Delay(200);
        await _retryPolicy.DidNotReceive().RetryAsync(2, Arg.Any<Func<Task>>());
    }


    private static readonly (byte[] rawBytes, IEnumerable<ProtocolMessage> messages) OneMessageLengthLessThan10 = (
        [1, 2, 255, 4, 1],
        new List<ProtocolMessage>
        {
            new()
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = [1],
            },
        });

    private static readonly (byte[] rawBytes, IEnumerable<ProtocolMessage> messages) OneMessageLengthGreaterThan10LessThan100 = (
        [1, 1, 1, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
        new List<ProtocolMessage>
        {
            new()
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
            },
        });

    private static readonly (byte[] rawBytes, IEnumerable<ProtocolMessage> messages) TwoMessagesOfBytes = (
        [1, 2, 255, 4, 1, 1, 1, 1, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
        new List<ProtocolMessage>
        {
            new()
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = [1],
            },
            new()
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
            },
        });

    private static IEnumerable<(byte[] rawBytes, IEnumerable<ProtocolMessage> messages)> ExtractMessagesFromBytesStrongTypeCases
    {
        get
        {
            yield return OneMessageLengthLessThan10;
            yield return OneMessageLengthGreaterThan10LessThan100;
            yield return TwoMessagesOfBytes;
        }
    }

    public static IEnumerable<object[]> ExtractMessagesFromBytesCases =>
        ExtractMessagesFromBytesStrongTypeCases.Select(x => new object[] { x.rawBytes, x.messages });

    [Theory]
    [MemberData(nameof(ExtractMessagesFromBytesCases))]
    public void ExtractMessagesFromBytes_WhenCalled_AlwaysPass(byte[] rawBytes, IEnumerable<ProtocolMessage> messages)
    {
        _adapter.ExtractMessagesFromBytes(rawBytes)
            .Should()
            .BeEquivalentTo(messages);
    }
}