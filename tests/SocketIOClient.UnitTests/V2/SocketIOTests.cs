using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.V2;
using SocketIOClient.V2.Serializer.SystemTextJson;
using SocketIOClient.V2.Session;
using Xunit;

namespace SocketIOClient.UnitTests.V2;

public class SocketIOTests
{
    public SocketIOTests()
    {
        _session = Substitute.For<ISession>();
        var sessionFactory = Substitute.For<ISessionFactory>();
        sessionFactory.New(Arg.Any<EngineIO>()).Returns(_session);
        _io = new SocketIOClient.V2.SocketIO("http://localhost:3000")
        {
            SessionFactory = sessionFactory,
        };
    }

    private readonly SocketIOClient.V2.SocketIO _io;
    private readonly ISession _session;

    [Fact]
    public async Task EmitAsync_NotConnected_ThrowException()
    {
        await _io.Invoking(x => x.EmitAsync("event", _ => { }))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SocketIO is not connected.");
    }

    [Fact]
    public async Task EmitAsync_AckEvent_PacketIdIncrementBy1()
    {
        await _io.ConnectAsync();
        await _io.EmitAsync("event", _ => { });

        _io.PacketId.Should().Be(1);
    }

    [Fact]
    public async Task EmitAsync_AckEventAndGotResponse_HandlerIsCalled()
    {
        var ackCalled = false;

        await _io.ConnectAsync();
        await _io.EmitAsync("event", _ => ackCalled = true);
        var ackMessage = new SystemJsonAckMessage
        {
            Id = _io.PacketId,
        };
        _io.OnNext(ackMessage);

        ackCalled.Should().BeTrue();
    }
}