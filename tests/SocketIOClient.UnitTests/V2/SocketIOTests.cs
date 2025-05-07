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

    [Fact]
    public async Task EmitAsync_NotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", _ => { }))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

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
    public async Task ConnectAsyncCancellationToken_CancelAfter200ms_ThrowConnectionException()
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

        _session.ConnectAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test"));
        await _io
            .Invoking(async x => await x.ConnectAsync())
            .Should()
            .ThrowAsync<ConnectionException>();
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
}