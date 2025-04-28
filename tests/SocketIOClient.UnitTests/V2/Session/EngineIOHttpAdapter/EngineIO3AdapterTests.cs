using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;

namespace SocketIOClient.UnitTests.V2.Session.EngineIOHttpAdapter;

public class EngineIO3AdapterTests
{
    public EngineIO3AdapterTests()
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _serializer = Substitute.For<ISerializer>();
        _protocolAdapter = Substitute.For<IProtocolAdapter>();
        _retryPolicy = Substitute.For<IRetriable>();
        _adapter = new(
            _stopwatch,
            _serializer,
            _protocolAdapter,
            TimeSpan.FromSeconds(1),
            _retryPolicy);
    }

    private readonly IStopwatch _stopwatch;
    private readonly ISerializer _serializer;
    private readonly IProtocolAdapter _protocolAdapter;
    private readonly EngineIO3Adapter _adapter;
    private readonly IRetriable _retryPolicy;

    [Fact]
    public void ToHttpRequest_GivenAnEmptyArray_ThrowException()
    {
        _adapter
            .Invoking(x => x.ToHttpRequest(new List<byte[]>()))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The array cannot be empty");
    }

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) OneItem1Byte = new(new List<byte[]>
    {
        new byte[] { 1 },
    }, new HttpRequest
    {
        BodyBytes = [1, 1, 255, 4, 1],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) OneItem10Bytes = new(new List<byte[]>
    {
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyBytes = [1, 1, 0, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) TwoItems1Byte10Bytes = new(new List<byte[]>
    {
        new byte[] { 1 },
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyBytes = [1, 1, 255, 4, 1, 1, 1, 0, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static IEnumerable<(ICollection<byte[]> bytes, IHttpRequest req)> ToHttpRequestStrongTypeCases
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
    public void ToHttpRequest_WhenCalled_AlwaysPass(ICollection<byte[]> bytes, IHttpRequest result)
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
        _adapter.GetMessages(raw)
            .Select(m => m.Text)
            .Should()
            .BeEquivalentTo(textMessages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToHttpRequest_GivenAnInvalidContent_ThrowException([CanBeNull] string? content)
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
        var ping = new ProtocolMessage
        {
            Text = "2",
        };
        _serializer.NewPingMessage().Returns(ping);
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        var range = Quantity.Within(9, 11);
        await _retryPolicy.Received(range).RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task ProcessMessageAsync_PongMessage_NotifyObserverWithDuration()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);
        _stopwatch.Elapsed.Returns(TimeSpan.FromSeconds(1));

        await _adapter.ProcessMessageAsync(new PongMessage());

        await observer
            .Received(1)
            .OnNextAsync(Arg.Is<IMessage>(m => ((PongMessage)m).Duration == TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task StartPingAsync_ObserverThrowException_ContinuePing()
    {
        var ping = new ProtocolMessage
        {
            Text = "2",
        };
        _serializer.NewPingMessage().Returns(ping);
        var observer = Substitute.For<IMyObserver<IMessage>>();
        observer.OnNextAsync(Arg.Any<IMessage>()).ThrowsAsync(new Exception());
        _adapter.Subscribe(observer);

        await _adapter.ProcessMessageAsync(new OpenedMessage
        {
            PingInterval = 10,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(100);

        var range = Quantity.Within(7, 12);
        await _retryPolicy.Received().RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }
}