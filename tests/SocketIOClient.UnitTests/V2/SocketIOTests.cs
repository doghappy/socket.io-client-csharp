using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Test.Core;
using SocketIOClient.V2;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.V2;

public class SocketIOTests
{
    public SocketIOTests(ITestOutputHelper output)
    {
        _session = Substitute.For<ISession>();
        _random = Substitute.For<IRandom>();
        _output = output;
        _io = NewSocketIO("http://localhost:3000");
    }

    private SocketIOClient.V2.SocketIO NewSocketIO(string url)
    {
        return new SocketIOClient.V2.SocketIO(url, services =>
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new XUnitLoggerProvider(_output));
            });
            services.Replace(ServiceDescriptor.KeyedScoped(TransportProtocol.Polling, (_, _) => _session));
            services.Replace(ServiceDescriptor.Singleton(_random));
        })
        {
            Options =
            {
                Reconnection = false,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
            },
        };
    }

    private readonly SocketIOClient.V2.SocketIO _io;
    private readonly ISession _session;
    private readonly IRandom _random;
    private readonly ITestOutputHelper _output;

    [Fact]
    public void NothingCalled_DefaultValues()
    {
        var io = new SocketIOClient.V2.SocketIO("http://localhost:3000");
        io.PacketId.Should().Be(0);
        io.Connected.Should().BeFalse();
        io.Id.Should().BeNull();
    }

    #region ConnectAsync

    [Fact]
    public async Task ConnectAsync_FailedToConnect_ThrowConnectionException()
    {
        _io.Options.Reconnection = false;
        _io.Options.ReconnectionAttempts = 1;
        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>()
            .WithMessage("Cannot connect to server 'http://localhost:3000/'");
    }

    [Fact]
    public async Task ConnectAsync_ReconnectionIsFalseAttemptsIs2_OnReconnectErrorInvoked1Time()
    {
        _io.Options.Reconnection = false;
        _io.Options.ReconnectionAttempts = 2;
        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        var times = 0;
        _io.OnReconnectError += (_, _) => times++;

        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();

        times.Should().Be(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task ConnectAsync_FailedToConnect_OnReconnectErrorInvokedByAttempts(int attempts)
    {
        _io.Options.Reconnection = true;
        _io.Options.ReconnectionAttempts = attempts;
        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        var times = 0;
        _io.OnReconnectError += (_, _) => times++;

        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();

        times.Should().Be(attempts);
    }

    [Fact]
    public async Task OnReconnectError_ThrowException_NotBlocked()
    {
        _io.Options.Reconnection = true;
        _io.Options.ReconnectionAttempts = 2;
        _session.ConnectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Test")), Task.CompletedTask);

        var times = 0;
        _io.OnReconnectError += (_, _) =>
        {
            times++;
            throw new InvalidOperationException();
        };

        await ConnectAsync();

        _io.Connected.Should().BeTrue();
        times.Should().Be(1);
    }

    [Fact]
    public async Task ConnectAsync_SessionSuccessfullyConnected_SessionSubscribeIO()
    {
        await ConnectAsync();
        _session.Received(1).Subscribe(_io);
    }

    [Fact]
    public async Task ConnectAsync_ConnectedMessageReceived_ConnectedIsTrueIdHasValue()
    {
        await ConnectAsync();
        _io.Connected.Should().BeTrue();
        _io.Id.Should().Be("123");
    }

    [Fact]
    public async Task ConnectAsync_ConnectedMessageDelay_ConnectAsyncIsSync()
    {
        var stopwatch = Stopwatch.StartNew();
        await ConnectAsync(200);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should()
            .BeGreaterThanOrEqualTo(100)
            .And.BeLessThan(300);
    }

    [Fact]
    public async Task ConnectAsync_CancellationTokenIsCanceled_TaskCanceledException()
    {
        await _io
            .Invoking(async x =>
            {
                using var cts = new CancellationTokenSource();
                await cts.CancelAsync();
                await x.ConnectAsync(cts.Token);
            })
            .Should()
            .ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task ConnectAsync_CancelAfter100ms_ThrowTaskCanceledException()
    {
        _io.Options.Reconnection = true;
        _random.Next(Arg.Any<int>()).Returns(10);

        await _io
            .Invoking(async x =>
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(100));
                await x.ConnectAsync(cts.Token);
            })
            .Should()
            .ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task ConnectAsync_SessionConnectAsyncThrow_ThrowConnectionException()
    {
        _session.ConnectAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Unknown error"));
        await _io
            .Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionException>()
            .WithMessage($"Cannot connect to server 'http://localhost:3000/'");
    }

    [Fact]
    public async Task ConnectAsync_GetSocketIOConnectionResultTimeout_ThrowTimeoutException()
    {
        await _io
            .Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<TimeoutException>();
    }

    private static async Task OnNextAsync(SocketIOClient.V2.SocketIO io, IMessage message)
    {
        IInternalSocketIO internalSocketIO = io;
        await internalSocketIO.OnNextAsync(message);
    }

    [Fact]
    public async Task ConnectAsync_ReceiveAnErrorEvent_OnErrorIsInvoked()
    {
        _io.Options.Reconnection = true;
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await OnNextAsync(_io, new ErrorMessage
            {
                Error = "Invalid QueryString",
            });
        });
        var errors = new List<string>();
        _io.OnError += (_, err) => errors.Add(err);

        await _io
            .Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionException>()
            .WithMessage("Invalid QueryString");

        errors.Should().Equal("Invalid QueryString");
    }

    [Fact]
    public async Task ConnectAsync_FirstFailedThenSuccess_ConnectedIsTrue()
    {
        _session.ConnectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Test")), Task.CompletedTask);
        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();

        await ConnectAsync();

        _io.Connected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_FirstSuccessThenFailed_ThrowConnectionException()
    {
        await ConnectAsync();
        await _io.DisconnectAsync();
        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));
        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();
    }

    [Fact]
    public async Task ConnectAsync_AlreadyConnected_FastReturn()
    {
        await ConnectAsync();

        var stopWatch = Stopwatch.StartNew();
        await _io.ConnectAsync();
        stopWatch.Stop();

        stopWatch.ElapsedMilliseconds.Should().BeLessThan(10);
    }

    [Theory]
    [InlineData(EngineIO.V3)]
    [InlineData(EngineIO.V4)]
    public async Task ConnectAsync_SessionOptionsEngine_SameAsOptionsEIO(EngineIO eio)
    {
        _io.Options.EIO = eio;

        await ConnectAsync();

        _session.Options.EngineIO.Should().Be(eio);
    }

    [Fact]
    public async Task ConnectAsync_CustomValues_PassCorrectValuesToSessionOptions()
    {
        _io.Options.Path = "/chat";
        _io.Options.ConnectionTimeout = TimeSpan.FromSeconds(3);
        _io.Options.Query = [new KeyValuePair<string, string>("id", "abc"),];
        _io.Options.ExtraHeaders = new Dictionary<string, string>
        {
            ["User-Agent"] = "Hello World!",
        };
        _io.Options.Auth = new { user = "admin", password = "123456" };

        await ConnectAsync();

        _session.Options.Should()
            .BeEquivalentTo(new SessionOptions
            {
                ServerUri = new Uri("http://localhost:3000"),
                Path = "/chat/",
                Query = [new KeyValuePair<string, string>("id", "abc")],
                ExtraHeaders = new Dictionary<string, string>
                {
                    ["User-Agent"] = "Hello World!",
                },
                Timeout = TimeSpan.FromSeconds(3),
                EngineIO = EngineIO.V4,
                Auth = new { user = "admin", password = "123456" }
            });
    }

    [Theory]
    [InlineData("http://localhost:3000", null)]
    [InlineData("http://localhost:3000/", null)]
    [InlineData("http://localhost:3000//", "")]
    [InlineData("http://localhost:3000///", "")]
    [InlineData("http://localhost:3000/test", "/test")]
    [InlineData("http://localhost:3000/test/", "/test")]
    public async Task ConnectAsync_DifferentUrls_SetCorrectNamespaceForSessionOptions(string url, string expectedNsp)
    {
        var io = NewSocketIO(url);

        await ConnectAsync(io);

        _session.Options.Namespace.Should().Be(expectedNsp);
    }

    [Fact]
    public async Task ConnectAsync_SessionConnectAsyncDelay100_CanEmit()
    {
        _session.ConnectAsync(Arg.Any<CancellationToken>())
            .Returns(async _ => await Task.Delay(100));
        await ConnectAsync();

        await _io.EmitAsync("event");
        await _session.Received().SendAsync(Arg.Any<object[]>(), CancellationToken.None);
    }

    #endregion

    #region Private Methods

    private async Task ConnectAsync()
    {
        await ConnectAsync(_io);
    }

    private async Task ConnectAsync(int ms)
    {
        await ConnectAsync(_io, ms);
    }

    private async Task ConnectAsync(SocketIOClient.V2.SocketIO io)
    {
        await ConnectAsync(io, 20);
    }

    private async Task ConnectAsync(SocketIOClient.V2.SocketIO io, int ms)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(ms);
            await OnNextAsync(io, new ConnectedMessage
            {
                Sid = "123",
            });
        });
        await io.ConnectAsync();
    }

    #endregion

    #region EmitAsync

    [Fact]
    public async Task EmitAsyncActionAck_NotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", _ => { }))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsyncFuncAck_NotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", _ => Task.CompletedTask))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsyncData_NotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", new List<object>()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task SendAckDataAsync_NotConnected_ThrowException()
    {
        IInternalSocketIO io = _io;
        await io.Invoking(x => x.SendAckDataAsync(1, new List<object>()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsyncActionAck_WhenCalled_PacketIdIncrementBy1()
    {
        await ConnectAsync();
        await _io.EmitAsync("event", _ => { });

        _io.PacketId.Should().Be(1);
    }

    [Fact]
    public async Task EmitAsync_AckEventActionAndGotResponse_HandlerIsCalled()
    {
        var ackCalled = false;

        await ConnectAsync();
        await _io.EmitAsync("event", _ => ackCalled = true);
        var ackMessage = new SystemJsonAckMessage
        {
            Id = _io.PacketId,
        };
        await OnNextAsync(_io, ackMessage);

        ackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EmitAsyncActionAck_EventAndCancellationToken_TokenIsNotNone()
    {
        await ConnectAsync();

        using var cts = new CancellationTokenSource();
        await _io.EmitAsync("event", _ => { }, cts.Token);

        await _session.Received()
            .SendAsync(
                Arg.Any<object[]>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(t => t != CancellationToken.None));
    }

    [Fact]
    public async Task EmitAsyncFuncAck_WhenCalled_PacketIdIncrementBy1()
    {
        await ConnectAsync();
        await _io.EmitAsync("event", _ => Task.CompletedTask);

        _io.PacketId.Should().Be(1);
    }

    [Fact]
    public async Task EmitAsync_AckEventFuncAndGotResponse_HandlerIsCalled()
    {
        var ackCalled = false;

        await ConnectAsync();
        await _io.EmitAsync("event", _ =>
        {
            ackCalled = true;
            return Task.CompletedTask;
        });
        var ackMessage = new SystemJsonAckMessage
        {
            Id = _io.PacketId,
        };
        await OnNextAsync(_io, ackMessage);

        ackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EmitAsyncFuncAck_DataAndCancellationTokenNone_AlwaysPass()
    {
        await ConnectAsync();

        await _io.EmitAsync("event", [1], _ => Task.CompletedTask, CancellationToken.None);
        await _session.Received()
            .SendAsync(
                Arg.Is<object[]>(x => x.Length == 2 && "event".Equals(x[0]) && 1.Equals(x[1])),
                Arg.Any<int>(),
                CancellationToken.None);
    }

    [Fact]
    public async Task EmitAsyncFuncAck_WithCustomCancellationToken_TokenIsNotNone()
    {
        await ConnectAsync();

        using var cts = new CancellationTokenSource();
        await _io.EmitAsync("event", _ => Task.CompletedTask, cts.Token);

        await _session.Received()
            .SendAsync(
                Arg.Any<object[]>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(t => t != CancellationToken.None));
    }

    [Fact]
    public async Task SendAckDataAsync_DataAndCancellationTokenNone_AlwaysPass()
    {
        await ConnectAsync();

        IInternalSocketIO io = _io;
        await io.SendAckDataAsync(1, [1], CancellationToken.None);
        await _session.Received()
            .SendAckDataAsync(
                Arg.Is<object[]>(x => x.Length == 1 && 1.Equals(x[0])),
                Arg.Any<int>(),
                CancellationToken.None);
    }

    [Fact]
    public async Task EmitAsync_DataIsNull_ThrowArgumentNullException()
    {
        await ConnectAsync();

        IEnumerable<object> data = null!;
        await _io.Invoking(x => x.EmitAsync("event", data))
            .Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'data')");
    }

    [Fact]
    public async Task EmitAsyncActionAck_WithNullData_ThrowArgumentNullException()
    {
        await ConnectAsync();

        await _io.Invoking(x => x.EmitAsync("event", null, _ => { }, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'data')");
    }

    [Fact]
    public async Task EmitAsyncFuncAck_WithNullData_ThrowArgumentNullException()
    {
        await ConnectAsync();

        await _io.Invoking(x => x.EmitAsync("event", null, _ => Task.CompletedTask, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'data')");
    }

    [Fact]
    public async Task SendAckDataAsync_WithNullData_ThrowArgumentNullException()
    {
        await ConnectAsync();

        IInternalSocketIO io = _io;
        await io.Invoking(x => x.SendAckDataAsync(1, null))
            .Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'data')");
    }

    [Fact]
    public async Task EmitAsync_DataIsEmpty_AlwaysPass()
    {
        await ConnectAsync();

        await _io.EmitAsync("event", new List<object>());
        await _session.Received()
            .SendAsync(
                Arg.Is<object[]>(x => x.Length == 1 && "event".Equals(x[0])),
                CancellationToken.None);
    }

    [Fact]
    public async Task EmitAsync_DataIsOnly1Item_AlwaysPass()
    {
        await ConnectAsync();

        await _io.EmitAsync("event", [1]);
        await _session.Received()
            .SendAsync(
                Arg.Is<object[]>(x => x.Length == 2 && "event".Equals(x[0]) && 1.Equals(x[1])),
                CancellationToken.None);
    }

    [Fact]
    public async Task EmitAsync_EventAndDataAndCancellationToken_TokenIsNotNone()
    {
        await ConnectAsync();

        using var cts = new CancellationTokenSource();
        await _io.EmitAsync("event", new List<object>(), cts.Token);
        await _session.Received()
            .SendAsync(
                Arg.Any<object[]>(),
                Arg.Is<CancellationToken>(t => t != CancellationToken.None));
    }

    [Fact]
    public async Task EmitAsync_OnlyEvent_AlwaysPass()
    {
        await ConnectAsync();

        await _io.EmitAsync("event");
        await _session.Received()
            .SendAsync(
                Arg.Is<object[]>(x => x.Length == 1 && "event".Equals(x[0])),
                CancellationToken.None);
    }

    [Fact]
    public async Task EmitAsync_EventAndCancellationToken_TokenIsNotNone()
    {
        await ConnectAsync();

        using var cts = new CancellationTokenSource();
        await _io.EmitAsync("event", cts.Token);
        await _session.Received()
            .SendAsync(
                Arg.Any<object[]>(),
                Arg.Is<CancellationToken>(t => t != CancellationToken.None));
    }

    [Fact]
    public async Task EmitAsyncActionAck_AndEmptyData_AlwaysPass()
    {
        await ConnectAsync();

        await _io.EmitAsync("event", [], _ => { });

        await _session.Received()
            .SendAsync(
                Arg.Is<object[]>(x => x.Length == 1 && "event".Equals(x[0])),
                Arg.Any<int>(),
                CancellationToken.None);
        _io.PacketId.Should().Be(1);
    }

    [Fact]
    public async Task EmitAsyncActionAck_And1Data_AlwaysPass()
    {
        await ConnectAsync();

        await _io.EmitAsync("event", [1], _ => { }, CancellationToken.None);

        await _session.Received()
            .SendAsync(
                Arg.Is<object[]>(x => x.Length == 2 && "event".Equals(x[0]) && 1.Equals(x[1])),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        _io.PacketId.Should().Be(1);
    }

    [Fact]
    public async Task EmitAsync_EventAndDataAndActionAckAndCancellationToken_TokenIsNotNone()
    {
        await ConnectAsync();

        using var cts = new CancellationTokenSource();
        await _io.EmitAsync("event", [], _ => { }, cts.Token);

        await _session.Received()
            .SendAsync(
                Arg.Any<object[]>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(t => t != CancellationToken.None));
    }

    #endregion

    #region Events

    [Fact]
    public async Task OnPing_PingMessageWasReceived_EventHandlerIsCalled()
    {
        var called = false;
        _io.OnPing += (_, _) => called = true;
        await ConnectAsync();

        await OnNextAsync(_io, new PingMessage());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task OnPong_PongMessageWasReceived_EventHandlerIsCalled()
    {
        TimeSpan? ts = null;
        _io.OnPong += (_, e) => ts = e;
        await ConnectAsync();

        await OnNextAsync(_io, new PongMessage
        {
            Duration = TimeSpan.FromSeconds(2),
        });

        ts.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task OnConnected_ConnectedToServer_EventShouldBeInvoked()
    {
        var triggered = false;
        _io.OnConnected += (_, _) => triggered = true;

        await ConnectAsync();

        triggered.Should().BeTrue();
    }

    [Fact]
    public async Task OnConnected_ThrowExceptionByUserCode_LibWorkAsExpected()
    {
        _io.OnConnected += (_, _) => throw new Exception("Test");

        await ConnectAsync();

        _io.Connected.Should().BeTrue();
    }

    [Fact]
    public async Task OnReconnectAttempt_ReconnectionIsFalse_InvokeOnce()
    {
        _session.ConnectAsync(Arg.Any<CancellationToken>()).Throws(new TimeoutException());
        var attempts = 0;
        _io.OnReconnectAttempt += (_, _) => attempts++;

        var func = async () => await ConnectAsync();

        await func.Should().ThrowAsync<ConnectionException>();
        attempts.Should().Be(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public async Task OnReconnectAttempt_ReconnectionIsTrue_InvokeExactTimes(int attempts)
    {
        _io.Options.Reconnection = true;
        _io.Options.ReconnectionAttempts = attempts;
        _session.ConnectAsync(Arg.Any<CancellationToken>()).Throws(new TimeoutException());
        var times = 0;
        _io.OnReconnectAttempt += (_, _) => times++;

        var func = async () => await ConnectAsync();

        await func.Should().ThrowAsync<ConnectionException>();
        times.Should().Be(attempts);
    }

    [Fact]
    public async Task OnReconnectAttempt_ThrowException_NotBlocked()
    {
        _io.Options.Reconnection = true;
        _io.Options.ReconnectionAttempts = 2;
        _session.ConnectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new TimeoutException()), Task.CompletedTask);
        var times = 0;
        _io.OnReconnectAttempt += (_, t) =>
        {
            times = t;
            throw new InvalidOperationException();
        };

        await ConnectAsync();

        _io.Connected.Should().BeTrue();
        times.Should().Be(2);
    }

    #endregion

    #region DisconnectAsync

    [Fact]
    public async Task DisconnectAsync_NeverConnected_ClearStatefulData()
    {
        await _io.DisconnectAsync();

        _io.Connected.Should().BeFalse();
        _io.Id.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectAsync_EverConnected_ClearStatefulData()
    {
        await ConnectAsync();
        await _io.DisconnectAsync();

        _io.Connected.Should().BeFalse();
        _io.Id.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectAsync_WhenCalled_OnDisconnectedWillBeCalled()
    {
        var times = 0;
        string? reason = null;
        _io.OnDisconnected += (_, e) =>
        {
            times++;
            reason = e;
        };
        await ConnectAsync();
        await _io.DisconnectAsync();

        times.Should().Be(1);
        reason.Should().Be("io client disconnect");
    }

    [Fact]
    public async Task DisconnectAsync_NeverConnected_DisconnectAsyncOfSessionIsNotCalled()
    {
        await _io.DisconnectAsync();
        await _session.DidNotReceive().DisconnectAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisconnectAsync_EverConnected_DisconnectAsyncOfSessionIsNotCalled()
    {
        await ConnectAsync();
        await _io.DisconnectAsync();
        await _session.Received().DisconnectAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DisconnectAsync_CancellationTokenOverload_DisconnectAsyncOfSessionIsNotCalled()
    {
        await ConnectAsync();
        using var cts = new CancellationTokenSource();
        await _io.DisconnectAsync(cts.Token);
        await _session.Received()
            .DisconnectAsync(Arg.Is<CancellationToken>(t => t != CancellationToken.None));
    }

    [Fact]
    public async Task DisconnectAsync_ExceptionOccurredWhileSendingDisconnectMessage_NotThrow()
    {
        await ConnectAsync();

        _session.DisconnectAsync(Arg.Any<CancellationToken>()).Throws(new Exception("Test"));

        await _io.Invoking(x => x.DisconnectAsync())
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task DisconnectAsync_TriggeredByServer_OnDisconnectedWillBeCalled()
    {
        string reason = null!;
        _io.OnDisconnected += (_, r) => reason = r;

        await ConnectAsync();
        await OnNextAsync(_io, new DisconnectedMessage());
        await Task.Delay(50);

        _io.Connected.Should().BeFalse();
        _io.Id.Should().BeNull();
        reason.Should().Be("io server disconnect");
    }

    #endregion

    #region On

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void On_InvalidEventName_ThrowArgumentException(string? eventName)
    {
        _io.Invoking(x => x.On(eventName, _ => Task.CompletedTask))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void On_HandlerIsNull_ThrowArgumentNullException()
    {
        Func<IEventContext, Task> handler = null!;
        _io.Invoking(x => x.On("abc", handler))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task On_WhenCalledAndEventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        _io.On("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(1);
    }

    [Fact]
    public async Task On_EventReceivedTwice_HandlerShouldBeCalledTwice()
    {
        var times = 0;
        _io.On("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(2);
    }

    [Fact]
    public async Task On_AnotherEventReceived_HandlerShouldNotBeCalled()
    {
        var times = 0;
        _io.On("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "another",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(0);
    }

    [Fact]
    public async Task On_DuplicatedEventHandlerAdded_ExecuteNewerHandler()
    {
        var handler1Called = false;
        var handler2Called = false;
        _io.On("event", _ =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        });
        _io.On("event", _ =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        handler1Called.Should().BeFalse();
        handler2Called.Should().BeTrue();
    }

    [Fact]
    public async Task On_BinaryEventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        _io.On("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonBinaryEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(1);
    }

    #endregion

    #region Once

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Once_InvalidEventName_ThrowArgumentException(string? eventName)
    {
        _io.Invoking(x => x.Once(eventName, _ => Task.CompletedTask))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void Once_HandlerIsNull_ThrowArgumentNullException()
    {
        Func<IEventContext, Task> handler = null!;
        _io.Invoking(x => x.Once("abc", handler))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Once_WhenCalledAndEventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        _io.Once("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(1);
    }

    [Fact]
    public async Task Once_EventReceivedTwice_HandlerShouldBeCalledOnce()
    {
        var times = 0;
        _io.Once("event", async _ =>
        {
            times++;
            await Task.Delay(50);
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await Task.WhenAll(OnNextAsync(_io, eventMessage), OnNextAsync(_io, eventMessage));

        times.Should().Be(1);
    }

    [Fact]
    public async Task Once_AnotherEventReceived_HandlerShouldNotBeCalled()
    {
        var times = 0;
        _io.Once("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "another",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(0);
    }

    [Fact]
    public async Task Once_DuplicatedEventHandlerAdded_ExecuteNewerHandler()
    {
        var handler1Called = false;
        var handler2Called = false;
        _io.Once("event", _ =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        });
        _io.Once("event", _ =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        handler1Called.Should().BeFalse();
        handler2Called.Should().BeTrue();
    }

    [Fact]
    public async Task Once_RegisterOnceEventLaterThanOn_UseOnceHandlerInstead()
    {
        var handler1Called = 0;
        var handler2Called = 0;
        _io.On("event", _ =>
        {
            handler1Called++;
            return Task.CompletedTask;
        });
        _io.Once("event", _ =>
        {
            handler2Called++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);
        await OnNextAsync(_io, eventMessage);

        handler1Called.Should().Be(0);
        handler2Called.Should().Be(1);
    }

    [Fact]
    public async Task Once_RegisterOnceEventEarlierThanOn_UseOnEventInstead()
    {
        var handler1Called = 0;
        var handler2Called = 0;
        _io.Once("event", _ =>
        {
            handler1Called++;
            return Task.CompletedTask;
        });
        _io.On("event", _ =>
        {
            handler2Called++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);
        await OnNextAsync(_io, eventMessage);

        handler1Called.Should().Be(0);
        handler2Called.Should().Be(2);
    }

    [Fact]
    public async Task Once_BinaryEventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        _io.Once("event", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonBinaryEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(1);
    }

    #endregion

    #region OnAny

    [Fact]
    public async Task OnAny_EventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        string eventName = null!;
        IEventContext? context = null;
        _io.OnAny((e, ctx) =>
        {
            times++;
            eventName = e;
            context = ctx;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [1],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(1);
        eventName.Should().Be("event");
        context.Should().NotBeNull();
        context.GetDataValue<int>(0).Should().Be(1);
    }

    [Fact]
    public async Task OnAny_2Handlers_Invoked2Times()
    {
        var times = 0;
        _io.OnAny((_, _) =>
        {
            times++;
            return Task.CompletedTask;
        });
        _io.OnAny((_, _) =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        times.Should().Be(2);
    }

    [Fact]
    public async Task OnAny_FirstHandlerThrows_SecondHandlerStillWorks()
    {
        string handler = null!;
        _io.OnAny((_, _) => throw new InvalidOperationException());
        _io.OnAny((_, _) =>
        {
            handler = "Second";
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        handler.Should().Be("Second");
    }

    [Fact]
    public async Task OnAny_OnAnyHandlerThrows_OnHandlerStillWorks()
    {
        string handler = null!;
        _io.OnAny((_, _) => throw new InvalidOperationException());
        _io.On("event", _ =>
        {
            handler = "OnHandler";
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        handler.Should().Be("OnHandler");
    }

    [Fact]
    public async Task OnAny_OnHandlerThrows_OnAnyHandlerStillWorks()
    {
        string handler = null!;
        _io.OnAny((_, _) =>
        {
            handler = "OnAnyHandler";
            return Task.CompletedTask;
        });
        _io.On("event", _ => throw new InvalidOperationException());

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        handler.Should().Be("OnAnyHandler");
    }

    [Fact]
    public async Task OnAny_2Handlers_ExecutionIsOrdered()
    {
        List<string> list = [];
        _io.OnAny(async (_, _) =>
        {
            await Task.Delay(20);
            list.Add("Handler1");
        });
        _io.OnAny((_, _) =>
        {
            list.Add("Handler2");
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await OnNextAsync(_io, eventMessage);

        list.Should().Equal("Handler1", "Handler2");
    }

    #endregion

    #region OnAny Off

    [Fact]
    public void OffAny_GivenNull_DoNothing()
    {
        _io.Invoking(x => x.OffAny(null))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void OffAny_GivenNotExistingHandler_DoNothing()
    {
        _io.OffAny((_, _) => Task.CompletedTask);
        _io.ListenersAny.Should().BeEmpty();
    }

    [Fact]
    public void OffAny_GivenExistingHandler_Remove()
    {
        var handler = (string eventName, IEventContext ctx) => Task.CompletedTask;
        _io.OnAny(handler);
        _io.OffAny(handler);

        _io.ListenersAny.Should().BeEmpty();
    }

    #endregion

    #region PrependAny

    [Fact]
    public void PrependAny_GivenNull_DoNothing()
    {
        _io.Invoking(x => x.PrependAny(null))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void PrependAny_NoHandlersThenRegisterAHandler_CountIs1()
    {
        _io.PrependAny((_, _) => Task.CompletedTask);
        _io.ListenersAny.Should().HaveCount(1);
    }

    [Fact]
    public void PrependAny_Given2DuplicatedHandlers_CountIs2()
    {
        var handler = (string eventName, IEventContext ctx) => Task.CompletedTask;
        _io.PrependAny(handler);
        _io.PrependAny(handler);

        _io.ListenersAny.Should().HaveCount(2);
    }

    #endregion

    #region Reconnect

    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public async Task Reconnect_Manually_OnConnectedAndOnDisconnectedAreWorks(int times)
    {
        var connectTimes = 0;
        var disconnectTimes = 0;
        _io.OnConnected += (_, _) => connectTimes++;
        _io.OnDisconnected += (_, _) => disconnectTimes++;

        for (var i = 0; i < times; i++)
        {
            await ConnectAsync();
            await _io.DisconnectAsync();
        }

        await Task.Delay(50);
        connectTimes.Should().Be(times);
        disconnectTimes.Should().Be(times);
    }

    #endregion
}