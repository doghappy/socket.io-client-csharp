using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;

namespace SocketIOClient.UnitTests.V2;

public class SocketIOTests
{
    public SocketIOTests()
    {
        _session = Substitute.For<ISession>();

        _sessionFactory = Substitute.For<ISessionFactory>();
        _sessionFactory.New(Arg.Any<EngineIO>(), Arg.Any<SessionOptions>()).Returns(_session);
        _random = Substitute.For<IRandom>();
        _io = new SocketIOClient.V2.SocketIO("http://localhost:3000")
        {
            SessionFactory = _sessionFactory,
            Random = _random,
            Options =
            {
                Reconnection = false,
            },
        };
    }

    private readonly SocketIOClient.V2.SocketIO _io;
    private readonly ISession _session;
    private readonly IRandom _random;
    private readonly ISessionFactory _sessionFactory;

    [Fact]
    public void NothingCalled_DefaultValues()
    {
        var io = new SocketIOClient.V2.SocketIO("http://localhost:3000");
        io.PacketId.Should().Be(0);
        io.Connected.Should().BeFalse();
        io.Id.Should().BeNull();
        io.SessionFactory.Should().BeOfType<DefaultSessionFactory>();
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
    public async Task ConnectAsync_ReconnectionIsFalseAttempsIs2_OnReconnectErrorInvoked1Time()
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
    public async Task ConnectAsync_FirstSuccess_ConnectAsyncOfSessionIsCalled1Time()
    {
        _io.Options.Reconnection = true;

        await ConnectAsync();
        await Task.Delay(2000);

        await _session.Received(1).ConnectAsync(Arg.Any<CancellationToken>());
        _sessionFactory.Received(1).New(Arg.Any<EngineIO>(), Arg.Any<SessionOptions>());
    }

    [Fact]
    public async Task ConnectAsync_ConnectedMessageDelay_ConnectAsyncIsSync()
    {
        var stopwatch = Stopwatch.StartNew();
        await ConnectAsync(200);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should()
            .BeGreaterThanOrEqualTo(200)
            .And.BeLessThan(280);
    }

    [Fact]
    public async Task ConnectAsyncCancellationToken_GivenACanceledToken_ThrowConnectionException()
    {
        await _io
            .Invoking(async x =>
            {
                using var cts = new CancellationTokenSource();
                await cts.CancelAsync();
                await x.ConnectAsync(cts.Token);
            })
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ConnectAsyncCancellationToken_CancelAfter100ms_ThrowOperationCanceledException()
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
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ConnectAsync_FailedToConnect_SessionIsDisposed()
    {
        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));

        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();

        _session.Received().Dispose();
    }

    [Fact]
    public async Task ConnectAsync_FirstFailedThenSuccess_ConnectedIsTrue()
    {
        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));
        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();

        _session.ConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
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
    public async Task ConnectAsync_AlreadyConnected_Skip()
    {
        await ConnectAsync();
        await _io.ConnectAsync();

        _sessionFactory.Received(1).New(Arg.Any<EngineIO>(), Arg.Any<SessionOptions>());
    }

    [Theory]
    [InlineData(EngineIO.V3)]
    [InlineData(EngineIO.V4)]
    public async Task ConnectAsync_DifferentEngineIO_PassCorrectValueToSessionFactory(EngineIO eio)
    {
        var options = new SocketIOClient.V2.SocketIOOptions
        {
            EIO = eio,
        };
        var io = new SocketIOClient.V2.SocketIO("http://localhost:3000", options)
        {
            SessionFactory = _sessionFactory,
            Random = _random,
        };

        await ConnectAsync(io);

        _sessionFactory.Received(1).New(eio, Arg.Any<SessionOptions>());
    }

    [Fact]
    public async Task ConnectAsync_CustomValues_PassCorrectValuesToSessionFactory()
    {
        var options = new SocketIOClient.V2.SocketIOOptions
        {
            Path = "/chat",
            ConnectionTimeout = TimeSpan.FromSeconds(30),
            Query =
            [
                new KeyValuePair<string, string>("id", "abc"),
            ],
        };
        var io = new SocketIOClient.V2.SocketIO("http://localhost:3000", options)
        {
            SessionFactory = _sessionFactory,
            Random = _random,
        };

        SessionOptions receivedSessionOptions = null!;
        _sessionFactory.When(x => x.New(Arg.Any<EngineIO>(), Arg.Any<SessionOptions>()))
            .Do(info => { receivedSessionOptions = info.Arg<SessionOptions>(); });

        await ConnectAsync(io);

        receivedSessionOptions.Should()
            .BeEquivalentTo(new SessionOptions
            {
                ServerUri = new Uri("http://localhost:3000"),
                Path = "/chat",
                Query =
                [
                    new KeyValuePair<string, string>("id", "abc"),
                ],
                Timeout = TimeSpan.FromSeconds(30),
            });
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

    private static async Task ConnectAsync(SocketIOClient.V2.SocketIO io)
    {
        await ConnectAsync(io, 0);
    }

    private static async Task ConnectAsync(SocketIOClient.V2.SocketIO io, int ms)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(ms);
            await io.OnNextAsync(new ConnectedMessage
            {
                Sid = "123",
            });
        });
        await io.ConnectAsync();
    }

    #endregion

    #region EmitAsync

    [Fact]
    public async Task EmitAsync_ActionAckNotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", _ => { }))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsync_FuncAckNotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", _ => Task.CompletedTask))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsync_DataNotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", new List<object>()))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsync_AckEventAction_PacketIdIncrementBy1()
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
        await _io.OnNextAsync(ackMessage);

        ackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EmitAsync_EventAndActionAckAndCancellationToken_TokenIsNotNone()
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
    public async Task EmitAsync_AckEventFunc_PacketIdIncrementBy1()
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
        await _io.OnNextAsync(ackMessage);

        ackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EmitAsync_EventAndDataAndFuncAck_AlwaysPass()
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
    public async Task EmitAsync_EventAndFuncAckAndCancellationToken_TokenIsNotNone()
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
    public async Task EmitAsync_ActionAckAndEmptyData_AlwaysPass()
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
    public async Task EmitAsync_ActionAckAnd1Data_AlwaysPass()
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

        await _io.OnNextAsync(new PingMessage());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task OnPong_PongMessageWasReceived_EventHandlerIsCalled()
    {
        TimeSpan? ts = null;
        _io.OnPong += (_, e) => ts = e;
        await ConnectAsync();

        await _io.OnNextAsync(new PongMessage
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

    #endregion

    #region DisconnectAsync

    [Fact]
    public async Task DisconnectAsync_NeverConnected_ClearStatefulData()
    {
        await _io.DisconnectAsync();

        _io.Connected.Should().BeFalse();
        _io.Id.Should().BeNull();
        _session.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task DisconnectAsync_EverConnected_ClearStatefulData()
    {
        await ConnectAsync();
        await _io.DisconnectAsync();

        _io.Connected.Should().BeFalse();
        _io.Id.Should().BeNull();
        _session.Received(1).Dispose();
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

    #endregion

    #region On

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OnAction_InvalidEventName_ThrowArgumentException(string? eventName)
    {
        _io.Invoking(x => x.On(eventName, _ => { }))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OnFunc_InvalidEventName_ThrowArgumentException(string? eventName)
    {
        _io.Invoking(x => x.On(eventName, _ => Task.CompletedTask))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void OnAction_HandlerIsNull_ThrowArgumentNullException()
    {
        _io.Invoking(x => x.On("abc", (Action<IAckMessage>)null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void OnFunc_HandlerIsNull_ThrowArgumentNullException()
    {
        _io.Invoking(x => x.On("abc", (Func<IAckMessage, Task>)null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task OnAction_WhenCalledAndEventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        _io.On("event", _ => times++);

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await _io.OnNextAsync(eventMessage);

        times.Should().Be(1);
    }

    [Fact]
    public async Task OnFunc_WhenCalledAndEventReceived_HandlerShouldBeCalled()
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
        await _io.OnNextAsync(eventMessage);

        times.Should().Be(1);
    }

    [Fact]
    public async Task OnAction_AnotherEventReceived_HandlerShouldNotBeCalled()
    {
        var times = 0;
        _io.On("another", _ => times++);

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await _io.OnNextAsync(eventMessage);

        times.Should().Be(0);
    }

    [Fact]
    public async Task OnFunc_AnotherEventReceived_HandlerShouldNotBeCalled()
    {
        var times = 0;
        _io.On("another", _ =>
        {
            times++;
            return Task.CompletedTask;
        });

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await _io.OnNextAsync(eventMessage);

        times.Should().Be(0);
    }

    [Fact]
    public async Task OnAction_DuplicatedEventHandlerAdded_ExecuteTheFirstHandler()
    {
        var handler1Called = false;
        var handler2Called = false;
        _io.On("event", _ => handler1Called = true);
        _io.On("event", _ => handler2Called = true);

        var eventMessage = new SystemJsonEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await _io.OnNextAsync(eventMessage);

        handler1Called.Should().BeTrue();
        handler2Called.Should().BeFalse();
    }

    [Fact]
    public async Task OnFunc_DuplicatedEventHandlerAdded_ExecuteTheFirstHandler()
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
        await _io.OnNextAsync(eventMessage);

        handler1Called.Should().BeTrue();
        handler2Called.Should().BeFalse();
    }

    [Fact]
    public void On_FirstAddFuncHandlerThenAddActionByUsingSameEventName_ThrowArgumentException()
    {
        _io.On("event", _ => Task.CompletedTask);
        _io.Invoking(x => x.On("event", _ => { }))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void On_FirstAddActionHandlerThenAddFuncByUsingSameEventName_ThrowArgumentException()
    {
        _io.On("event", _ => { });
        _io.Invoking(x => x.On("event", _ => Task.CompletedTask))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public async Task On_BinaryEventReceived_HandlerShouldBeCalled()
    {
        var times = 0;
        _io.On("event", _ => times++);

        var eventMessage = new SystemJsonBinaryEventMessage
        {
            Event = "event",
            DataItems = [],
        };
        await _io.OnNextAsync(eventMessage);

        times.Should().Be(1);
    }

    #endregion
}