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
        var sessionFactory = Substitute.For<ISessionFactory>();
        sessionFactory.New(Arg.Any<EngineIO>(), Arg.Any<SessionOptions>()).Returns(_session);
        _random = Substitute.For<IRandom>();
        _io = new SocketIOClient.V2.SocketIO("http://localhost:3000")
        {
            SessionFactory = sessionFactory,
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
        await _io.ConnectAsync();
        _session.Received(1).Subscribe(_io);
    }

    [Fact]
    public async Task ConnectAsync_SessionSuccessfullyConnectedButNoConnectedMessage_ConnectedIsFalse()
    {
        await _io.ConnectAsync();
        _io.Connected.Should().BeFalse();
    }

    private async Task ConnectAsync()
    {
        await _io.ConnectAsync();
        await _io.OnNextAsync(new ConnectedMessage
        {
            Sid = "123",
        });
    }

    [Fact]
    public async Task ConnectAsync_ConnectedMessageReceived_ConnectedIsTrueIdHasValue()
    {
        await ConnectAsync();
        _io.Connected.Should().BeTrue();
        _io.Id.Should().Be("123");
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
}