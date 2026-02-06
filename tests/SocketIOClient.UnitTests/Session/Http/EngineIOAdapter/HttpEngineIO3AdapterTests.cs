using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Infrastructure;
using SocketIOClient.Observers;
using SocketIOClient.Protocol.Http;
using SocketIOClient.Session.EngineIOAdapter;
using SocketIOClient.Session.Http.EngineIOAdapter;
using SocketIOClient.Test.Core;
using SocketIOClient.UnitTests.Fakes;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Session.Http.EngineIOAdapter;

public class HttpEngineIO3AdapterTests
{
    public HttpEngineIO3AdapterTests(ITestOutputHelper output)
    {
        _stopwatch = Substitute.For<IStopwatch>();
        _httpAdapter = Substitute.For<IHttpAdapter>();
        _httpAdapter.IsReadyToSend.Returns(true);
        _retryPolicy = Substitute.For<IRetriable>();
        _retryPolicy.RetryAsync(2, Arg.Any<Func<Task>>())
            .Returns(async _ => await Task.Delay(50).ConfigureAwait(false));
        var logger = output.CreateLogger<HttpEngineIO3Adapter>();
        _pollingHandler = Substitute.For<IPollingHandler>();
        _fakeDelay = new FakeDelay(output);
        _adapter = new(
            _stopwatch,
            _httpAdapter,
            _retryPolicy,
            logger,
            _pollingHandler,
            _fakeDelay)
        {
            Options = new EngineIOAdapterOptions()
        };
    }

    private readonly IStopwatch _stopwatch;
    private readonly IHttpAdapter _httpAdapter;
    private readonly HttpEngineIO3Adapter _adapter;
    private readonly IRetriable _retryPolicy;
    private readonly IPollingHandler _pollingHandler;
    private readonly FakeDelay _fakeDelay;

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
            .Invoking(x => x.ToHttpRequest(content!))
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
            PingInterval = 100,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await _fakeDelay.AdvanceAsync(100, 3);
        var range = Quantity.Within(2, 3);
        await _retryPolicy.Received(range).RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .Received(range)
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
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
            PingInterval = 100,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await _fakeDelay.AdvanceAsync(100, 3);

        var range = Quantity.Within(2, 3);
        await _retryPolicy.Received(range).RetryAsync(3, Arg.Any<Func<Task>>());
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
            PingInterval = 400,
        });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(50).ConfigureAwait(false);

        await _retryPolicy.DidNotReceive().RetryAsync(3, Arg.Any<Func<Task>>());
        await observer
            .DidNotReceive()
            .OnNextAsync(Arg.Is<IMessage>(m => m.Type == MessageType.Ping));
    }

    [Fact]
    public async Task StartPingAsync_IsNotReadyToSend_DidNotPing()
    {
        _pollingHandler.WaitHttpAdapterReady().Returns(async _ => await Task.Delay(2000));

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(200).ConfigureAwait(false);

        await _retryPolicy.DidNotReceive().RetryAsync(Arg.Any<int>(), Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task ProcessMessageAsync_IsReadyAfter30ms_PingIsWorking()
    {
        _pollingHandler.WaitHttpAdapterReady().Returns(async _ => await Task.Delay(30));
        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await _fakeDelay.AdvanceAsync(100);
        await _retryPolicy.Received().RetryAsync(3, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task StartPingAsync_DisposeIsCalled_NeverPing()
    {
        _adapter.Dispose();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });
        await _adapter.ProcessMessageAsync(new ConnectedMessage());

        await Task.Delay(200).ConfigureAwait(false);

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
    public async Task PollingAsync_DisposeIsCalled_NeverPolling()
    {
        _adapter.Dispose();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 100 });

        await Task.Delay(200).ConfigureAwait(false);
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

    private static readonly (byte[] rawBytes, IEnumerable<ProtocolMessage> messages)
        OneMessageLengthGreaterThan10LessThan100 = (
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

    private static readonly (byte[] rawBytes, IEnumerable<ProtocolMessage> messages) OneTextOneBytes = (
        [
            0x00, 0x01, 0x00, 0x05, 0xFF, 0x34, 0x35, 0x31, 0x2D, 0x5B, 0x22, 0x65, 0x6E, 0x64, 0x2D, 0x61, 0x63, 0x6B,
            0x2D, 0x6F, 0x6E, 0x2D, 0x63, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x22, 0x2C, 0x7B, 0x22, 0x53, 0x69, 0x7A, 0x65,
            0x22, 0x3A, 0x31, 0x30, 0x32, 0x34, 0x2C, 0x22, 0x4E, 0x61, 0x6D, 0x65, 0x22, 0x3A, 0x22, 0x69, 0x6E, 0x64,
            0x65, 0x78, 0x2E, 0x68, 0x74, 0x6D, 0x6C, 0x22, 0x2C, 0x22, 0x42, 0x79, 0x74, 0x65, 0x73, 0x22, 0x3A, 0x7B,
            0x22, 0x5F, 0x70, 0x6C, 0x61, 0x63, 0x65, 0x68, 0x6F, 0x6C, 0x64, 0x65, 0x72, 0x22, 0x3A, 0x74, 0x72, 0x75,
            0x65, 0x2C, 0x22, 0x6E, 0x75, 0x6D, 0x22, 0x3A, 0x30, 0x7D, 0x7D, 0x2C, 0x22, 0x68, 0x65, 0x6C, 0x6C, 0x6F,
            0x22, 0x5D, 0x01, 0x01, 0x03, 0xFF, 0x04, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64,
            0x21
        ],
        new List<ProtocolMessage>
        {
            new()
            {
                Type = ProtocolMessageType.Text,
                Text = "451-[\"end-ack-on-client\",{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":{\"_placeholder\":true,\"num\":0}},\"hello\"]"
            },
            new()
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = TestFile.IndexHtml.Bytes,
            },
        });

    private static IEnumerable<(byte[] rawBytes, IEnumerable<ProtocolMessage> messages)>
        ExtractMessagesFromBytesStrongTypeCases
    {
        get
        {
            yield return OneMessageLengthLessThan10;
            yield return OneMessageLengthGreaterThan10LessThan100;
            yield return TwoMessagesOfBytes;
            yield return OneTextOneBytes;
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

    [Theory]
    [InlineData(null, "40")]
    [InlineData("", "40")]
    public async Task ProcessMessageAsync_ReceivedOpenedMessage_NotSendConnectedMessage(string nsp, string expected)
    {
        _adapter.Options.Namespace = nsp;
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _httpAdapter.DidNotReceive()
            .SendAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ReceivedOpenedMessage_SendConnectedMessage()
    {
        _adapter.Options.Namespace = "/nsp";
        await _adapter.ProcessMessageAsync(new OpenedMessage());

        await _httpAdapter.Received()
            .SendAsync(Arg.Is<HttpRequest>(r => r.BodyText == "7:40/nsp,"),
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/nsp", null, true)]
    [InlineData("/nsp", "", true)]
    [InlineData("/nsp", "/", true)]
    [InlineData("/nsp", "/abc", true)]
    [InlineData("/nsp", "/nsp", false)]
    [InlineData("/nsp", "/NSP", false)]
    [InlineData(null, null, false)]
    [InlineData("", null, false)]
    [InlineData(null, "", false)]
    [InlineData("", "/", false)]
    [InlineData("/", "", false)]
    [InlineData("/", null, false)]
    public async Task ProcessMessageAsync_NamespaceAndWhetherSwallow_AlwaysPass(string adapterNsp, string connNsp,
        bool shouldSwallow)
    {
        _adapter.Options.Namespace = adapterNsp;
        var message = new ConnectedMessage
        {
            Namespace = connNsp,
        };

        await _adapter.ProcessMessageAsync(new OpenedMessage());
        var result = await _adapter.ProcessMessageAsync(message);

        result.Should().Be(shouldSwallow);
    }

    [Fact]
    public async Task ProcessMessageAsync_OnlyReceivedSwallowedConnectedMessage_NeverStartPing()
    {
        _adapter.Options.Namespace = "/nsp";
        var message = new ConnectedMessage();

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 10 });
        await _adapter.ProcessMessageAsync(message);
        await _fakeDelay.EnsureNoDelayAsync(200);

        await _retryPolicy.DidNotReceive().RetryAsync(3, Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task ProcessMessageAsync_ReceivedConnectedMessageWithNamespace_StartPing()
    {
        _adapter.Options.Namespace = "/nsp";
        var message = new ConnectedMessage
        {
            Namespace = "/nsp",
        };

        await _adapter.ProcessMessageAsync(new OpenedMessage { PingInterval = 10 });
        await _adapter.ProcessMessageAsync(message);
        await _fakeDelay.AdvanceAsync(10);
        await _fakeDelay.AdvanceAsync(10);

        await _retryPolicy.Received().RetryAsync(3, Arg.Any<Func<Task>>());
    }
}