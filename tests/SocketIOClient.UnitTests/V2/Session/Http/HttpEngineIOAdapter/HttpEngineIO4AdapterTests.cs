using FluentAssertions;
using NSubstitute;
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

public class HttpEngineIO4AdapterTests
{
    public HttpEngineIO4AdapterTests(ITestOutputHelper output)
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _httpAdapter.IsReadyToSend.Returns(true);
        _retryPolicy = Substitute.For<IRetriable>();
        _retryPolicy.RetryAsync(2, Arg.Any<Func<Task>>()).Returns(async _ =>
        {
            await Task.Delay(50);
        });
        var logger = output.CreateLogger<HttpEngineIO4Adapter>();
        _adapter = new HttpEngineIO4Adapter(
            _stopwatch,
            _httpAdapter,
            _retryPolicy,
            logger);
    }

    private readonly IStopwatch _stopwatch;
    private readonly HttpEngineIO4Adapter _adapter;
    private readonly IHttpAdapter _httpAdapter;
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
        _adapter.Namespace = nsp;
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
}