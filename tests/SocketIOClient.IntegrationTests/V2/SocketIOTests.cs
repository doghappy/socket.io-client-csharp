using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Test.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2;
using SocketIOClient.Core;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2;

// TODO: to xUnit
// [TestClass]
public class SocketIOTests
{
    private readonly ITestOutputHelper _output;

    public SocketIOTests(ITestOutputHelper output)
    {
        _output = output;
        // localhost.charlesproxy.com
        _io = NewSocketIO("http://localhost:11210");
    }

    private readonly SocketIOClient.V2.SocketIOOptions _options = new()
    {
        EIO = EngineIO.V3,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };

    private readonly SocketIOClient.V2.SocketIO _io;
    private const int DefaultDelay = 200;
    private const string TokenUrl = "http://localhost:11211";

    private SocketIOClient.V2.SocketIO NewSocketIO(string url)
    {
        return new SocketIOClient.V2.SocketIO(url, _options, services =>
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new XUnitLoggerProvider(_output));
            });
        });
    }

    [Fact]
    public async Task ConnectAsync_ConnectedToServer_ConnectedIsTureIdIsNotNullOrEmpty()
    {
        await _io.ConnectAsync();

        _io.Connected.Should().BeTrue();
        _io.Id.Should().NotBeNullOrEmpty();
    }

    #region Emit

    [Fact]
    public async Task EmitAsync_EventNull_ReceiveNull()
    {
        IEventContext message = null!;
        _io.On("1:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await _io.ConnectAsync();
        await _io.EmitAsync("1:emit", [null]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        var receivedData = message.GetDataValue<object>(0);
        receivedData.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(-1234567890)]
    [InlineData(1234567890)]
    [InlineData(-1.234567890)]
    [InlineData(1.234567890)]
    [InlineData("hello\n世界\n🌍🌎🌏")]
    public async Task EmitAsync_Event1Parameter_ReceiveSameParameter(object data)
    {
        IEventContext message = null!;
        _io.On("1:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await _io.ConnectAsync();
        await _io.EmitAsync("1:emit", [data]);

        await Task.Delay(DefaultDelay);

        // TODO: json?
        message.Should().NotBeNull();
        message.GetDataValue(data.GetType(), 0)
            .Should()
            .BeEquivalentTo(data);
    }

    [Fact]
    public async Task EmitAsync_ByteEvent1Parameter_ReceiveSameParameter()
    {
        IEventContext message = null!;
        _io.On("1:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await _io.ConnectAsync();
        await _io.EmitAsync("1:emit", [TestFile.NiuB]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<TestFile>(0)
            .Should()
            .BeEquivalentTo(TestFile.NiuB);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, 123)]
    [InlineData(-1234567890, "test")]
    [InlineData("hello\n世界\n🌍🌎🌏", 199)]
    public async Task EmitAsync_Event2Parameters_ReceiveSameParameters(object item0, object item1)
    {
        IEventContext message = null!;
        _io.On("2:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await _io.ConnectAsync();
        await _io.EmitAsync("2:emit", [item0, item1]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue(item0.GetType(), 0)
            .Should()
            .BeEquivalentTo(item0);
        message.GetDataValue(item1.GetType(), 1)
            .Should()
            .BeEquivalentTo(item1);
    }

    [Fact]
    public async Task EmitAsync_ActionAckWith1Parameter_ReceiveSameParameter()
    {
        IDataMessage message = null!;
        await _io.ConnectAsync();
        await _io.EmitAsync("1:ack", ["action"], msg => message = msg);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<string>(0)
            .Should()
            .BeEquivalentTo("action");
    }

    [Fact]
    public async Task EmitAsync_FuncAckWith1Parameter_ReceiveSameParameter()
    {
        IDataMessage message = null!;
        await _io.ConnectAsync();
        await _io.EmitAsync("1:ack", [TestFile.NiuB], msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<TestFile>(0)
            .Should()
            .BeEquivalentTo(TestFile.NiuB);
    }

    #endregion

    [Theory]
    [InlineData(4000, 0, 0)]
    [InlineData(5900, 1, 1)]
    [InlineData(14000, 2, 2)]
    public async Task OnPingAndOnPong_HandlerAreRegistered_WorkAsExpected(int ms, int expectedPingTimes, int expectedPongTimes)
    {
        var pingTimes = 0;
        var pongTimes = 0;
        _io.OnPing += (_, _) => pingTimes++;
        _io.OnPong += (_, _) => pongTimes++;

        await _io.ConnectAsync();

        await Task.Delay(ms);
        pingTimes.Should().Be(expectedPingTimes);
        pongTimes.Should().Be(expectedPongTimes);
    }

    [Fact]
    public async Task ConnectAsync_ConnectAfterDisconnect_OnConnectedTimeIs2()
    {
        var times = 0;
        _io.OnConnected += (_, _) => times++;

        await _io.ConnectAsync();
        await _io.DisconnectAsync();
        await _io.ConnectAsync();

        await Task.Delay(100);

        times.Should().Be(2);
    }

    [Fact]
    public async Task SendAckDataAsync_ClientSend2Args_ServerExecuteCallback()
    {
        IEventContext message = null!;
        _io.On("ack-on-client", async data =>
        {
            await data.SendAckDataAsync([1, 2]);
        });
        _io.On("end-ack-on-client", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await _io.ConnectAsync();
        await _io.EmitAsync("begin-ack-on-client");

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<int>(0).Should().Be(1);
        message.GetDataValue<int>(1).Should().Be(2);
    }

    [Fact]
    public async Task SendAckDataAsync_ClientSendBytes_ServerExecuteCallback()
    {
        IEventContext message = null!;
        _io.On("ack-on-client", async data =>
        {
            await data.SendAckDataAsync([TestFile.IndexHtml, "hello"], CancellationToken.None);
        });
        _io.On("end-ack-on-client", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await _io.ConnectAsync();
        await _io.EmitAsync("begin-ack-on-client");

        await Task.Delay(DefaultDelay * 4);

        message.Should().NotBeNull();
        message.GetDataValue<TestFile>(0).Should().BeEquivalentTo(TestFile.IndexHtml);
        message.GetDataValue<string>(1).Should().Be("hello");
    }

    [Fact]
    public async Task ConnectAsync_InvalidQueryValue_InvokeOnError()
    {
        var errors = new List<string>();
        var io = NewSocketIO(TokenUrl);
        io.Options.Reconnection = false;
        io.Options.Query =
        [
            new KeyValuePair<string, string>("token", "invalid_token"),
        ];
        io.OnError += (_, err) => errors.Add(err);

        await io
            .Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionException>()
            .WithMessage("Authentication error");

        io.Connected.Should().BeFalse();
        errors.Should().Equal("Authentication error");
    }

    [Fact]
    public async Task ConnectAsync_ValidQueryValue_ConnectSuccess()
    {
        var io = NewSocketIO(TokenUrl);
        io.Options.Reconnection = false;
        io.Options.Query =
        [
            new KeyValuePair<string, string>("token", "abc"),
        ];

        await io.ConnectAsync(CancellationToken.None);

        io.Connected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_CalledByClient_OnDisconnectIsInvoked()
    {
        var times = 0;
        string? reason = null;
        _io.OnDisconnected += (_, e) =>
        {
            times++;
            reason = e;
        };

        await _io.ConnectAsync();
        await _io.DisconnectAsync();

        times.Should().Be(1);
        reason.Should().Be(DisconnectReason.IOClientDisconnect);
        _io.Id.Should().BeNull();
        _io.Connected.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_CalledByServer_OnDisconnectIsInvoked()
    {
        var times = 0;
        string? reason = null;
        _io.OnDisconnected += (_, e) =>
        {
            times++;
            reason = e;
        };

        await _io.ConnectAsync();
        await _io.EmitAsync("disconnect", [false]);
        await Task.Delay(100);

        times.Should().Be(1);
        reason.Should().Be(DisconnectReason.IOServerDisconnect);
        _io.Id.Should().BeNull();
        _io.Connected.Should().BeFalse();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public async Task Reconnect_Manually_OnConnectedAndOnDisconnectedTriggeredManyTimes(int times)
    {
        var connectTimes = 0;
        var disconnectTimes = 0;
        _io.OnConnected += (_, _) => connectTimes++;
        _io.OnDisconnected += (_, _) => disconnectTimes++;

        for (var i = 0; i < times; i++)
        {
            await _io.ConnectAsync();
            await _io.DisconnectAsync();
        }

        connectTimes.Should().Be(times);
        disconnectTimes.Should().Be(times);
    }

    [Theory]
    [InlineData("X-Custom-Header", "CustomHeader-Value")]
    [InlineData("User-Agent", "dotnet-socketio[client]/socket")]
    [InlineData("user-agent", "dotnet-socketio[client]/socket")]
    public async Task ExtraHeaders_UserGivenHeaders_PassThroughToServer(string key, string value)
    {
        string? actual = null;
        _io.Options.ExtraHeaders = new Dictionary<string, string>
        {
            { key, value },
        };

        await _io.ConnectAsync();
        var lowerCaseKey = key.ToLowerInvariant(); // limited by server
        await _io.EmitAsync("get_header", [lowerCaseKey], res =>
        {
            actual = res.GetDataValue<string>(0);
        });
        await Task.Delay(100);

        actual.Should().Be(value);
    }

    [Fact]
    public async Task OnAny_ReceivedEventMessage_HandlerIsCalled()
    {
        string? eventName = null;
        IEventContext context = null!;
        _io.OnAny((e, ctx) =>
        {
            eventName = e;
            context = ctx;
            return Task.CompletedTask;
        });

        await _io.ConnectAsync();
        await _io.EmitAsync("1:emit", ["OnAny"]);
        await Task.Delay(100);

        eventName.Should().Be("1:emit");
        context.GetDataValue<string>(0).Should().Be("OnAny");
    }

    [Fact]
    public async Task OnAny_OnHandlerAndOnAnyHandler_2HandlersAreCalled()
    {
        var onHandlerCalled = false;
        var onAnyHandlerCalled = false;

        _io.OnAny((_, _) =>
        {
            onAnyHandlerCalled = true;
            return Task.CompletedTask;
        });
        _io.On("1:emit", _ =>
        {
            onHandlerCalled = true;
            return Task.CompletedTask;
        });

        await _io.ConnectAsync();
        await _io.EmitAsync("1:emit", ["OnAny"]);
        await Task.Delay(100);

        onHandlerCalled.Should().BeTrue();
        onAnyHandlerCalled.Should().BeTrue();
    }
}