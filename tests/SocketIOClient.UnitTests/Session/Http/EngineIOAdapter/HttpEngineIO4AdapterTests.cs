using FluentAssertions;
using NSubstitute;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.SystemTextJson;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.Http.EngineIOAdapter;

namespace SocketIOClient.UnitTests.Session.Http.EngineIOAdapter;

public class HttpEngineIO4AdapterTests
{
    public HttpEngineIO4AdapterTests()
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _httpAdapter.IsReadyToSend.Returns(true);
        var retryPolicy = Substitute.For<IRetriable>();
        retryPolicy.RetryAsync(2, Arg.Any<Func<Task>>()).Returns(async _ =>
        {
            await Task.Delay(50);
        });
        _serializer = Substitute.For<ISerializer>();
        _pollingHandler = Substitute.For<IPollingHandler>();
        _adapter = new HttpEngineIO4Adapter(_stopwatch, _httpAdapter, retryPolicy, _serializer, _pollingHandler)
        {
            Options = new EngineIOAdapterOptions()
        };
    }

    private readonly IStopwatch _stopwatch;
    private readonly HttpEngineIO4Adapter _adapter;
    private readonly IHttpAdapter _httpAdapter;
    private readonly ISerializer _serializer;
    private readonly IPollingHandler _pollingHandler;

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
        BodyText = "bAQ==",
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Text,
    });

    private static readonly (ICollection<byte[]> bytes, HttpRequest req) OneItem10Bytes = new(new List<byte[]>
    {
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyText = "bAAECAwQFBgcICQ==",
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Text,
    });

    private static readonly (ICollection<byte[]> bytes, HttpRequest req) TwoItems1Byte10Bytes = new(new List<byte[]>
    {
        new byte[] { 1 },
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyText = "bAQ==\u001EbAAECAwQFBgcICQ==",
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Text,
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

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) GetMessagesSingleHelloWorld = new(
        "hello world!",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "hello world!" },
        ]);

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) GetMessagesPingAndHelloWorld = new(
        "2\u001Ehello world!",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "2" },
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "hello world!" },
        ]);

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) GetMessagesPingAndBytes = new(
        "2\u001EbAQ==",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "2" },
            new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [1] },
        ]);

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) Get2Bytes = new(
        "bAA==\u001EbAQ==",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [0] },
            new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [1] },
        ]);

    public static TheoryData<string, IEnumerable<ProtocolMessage>> GetMessagesCases =>
        new()
        {
            {
                GetMessagesSingleHelloWorld.raw,
                GetMessagesSingleHelloWorld.messages
            },
            {
                GetMessagesPingAndHelloWorld.raw,
                GetMessagesPingAndHelloWorld.messages
            },
            {
                GetMessagesPingAndBytes.raw,
                GetMessagesPingAndBytes.messages
            },
            {
                Get2Bytes.raw,
                Get2Bytes.messages
            },
        };

    [Theory]
    [MemberData(nameof(GetMessagesCases))]
    public void GetMessages_WhenCalled_AlwaysPass(string raw, IEnumerable<ProtocolMessage> messages)
    {
        _adapter.ExtractMessagesFromText(raw)
            .Should()
            .BeEquivalentTo(messages);
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
    [InlineData(" ", " ")]
    [InlineData("hello, world!", "hello, world!")]
    public void ToHttpRequest_GivenValidContent_ReturnSameBodyText(string content, string expected)
    {
        var req = _adapter.ToHttpRequest(content);
        req.BodyText.Should().Be(expected);
    }

    [Fact]
    public async Task ProcessMessageAsync_PingMessage_NotifyObserverWithDuration()
    {
        var observer = Substitute.For<IMyObserver<IMessage>>();
        _adapter.Subscribe(observer);
        _stopwatch.Elapsed.Returns(TimeSpan.FromSeconds(1));

        await _adapter.ProcessMessageAsync(new PingMessage());

        await observer
            .Received(1)
            .OnNextAsync(Arg.Is<IMessage>(m => ((PongMessage)m).Duration == TimeSpan.FromSeconds(1)));
    }

    [Theory]
    [InlineData(null, "40")]
    [InlineData("", "40")]
    [InlineData("/nsp", "40/nsp,")]
    public async Task ProcessMessageAsync_ReceivedOpenedMessage_SendConnectedMessage(string nsp, string expected)
    {
        _adapter.Options.Namespace = nsp;
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _httpAdapter.Received()
            .SendAsync(Arg.Is<HttpRequest>(r => r.BodyText == expected), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "40{auth}")]
    [InlineData("/nsp", "40/nsp,{auth}")]
    public async Task ProcessMessageAsync_AuthIsProvided_ConnectedMessageContainsAuth(string nsp, string expected)
    {
        _adapter.Options.Namespace = nsp;
        _adapter.Options.Auth = new { user = "admin", password = "123456" };
        _serializer.Serialize(Arg.Any<object>()).Returns("{auth}");
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _httpAdapter.Received()
            .SendAsync(Arg.Is<HttpRequest>(r => r.BodyText == expected), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ExtractMessagesFromBytes_WhenCalled_AlwaysReturnEmpty()
    {
        _adapter.ExtractMessagesFromBytes([1, 2, 255, 4, 1])
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/nsp")]
    public async Task ProcessMessageAsync_OpenedMessage_StartPolling(string? nsp)
    {
        _adapter.Options.Namespace = nsp;
        var message = new OpenedMessage();
        await _adapter.ProcessMessageAsync(message);

        _pollingHandler.Received().StartPolling(message, false);
    }

    private static IEnumerable<IMessage> ProcessMessageAsyncMessageTypeTupleCases()
    {
        yield return new OpenedMessage();
        yield return new PingMessage();
        yield return new PongMessage();
        yield return new ConnectedMessage();
        yield return new DisconnectedMessage();
        yield return new SystemJsonEventMessage();
        yield return new SystemJsonAckMessage();
        yield return new ErrorMessage();
        yield return new SystemJsonBinaryEventMessage();
        yield return new SystemJsonBinaryAckMessage();
    }

    public static IEnumerable<object[]> ProcessMessageAsyncMessageTypeCases =>
        ProcessMessageAsyncMessageTypeTupleCases().Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(ProcessMessageAsyncMessageTypeCases))]
    public async Task ProcessMessageAsync_RegardlessOfMessageType_AlwaysReturnFalse(IMessage message)
    {
        var result = await _adapter.ProcessMessageAsync(message);
        result.Should().BeFalse();
    }
}