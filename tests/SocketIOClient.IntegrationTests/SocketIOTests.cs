using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common.Messages;
using SocketIOClient.Exceptions;
using SocketIOClient.Test.Core;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests;

public abstract class SocketIOTests(ITestOutputHelper output)
{
    protected abstract Uri Url { get; }
    protected abstract Uri TokenUrl { get; }
    protected abstract SocketIOOptions Options { get; }

    protected const int DefaultDelay = 500;

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected SocketIO NewSocketIO(Uri url)
    {
        return new SocketIO(url, Options, services =>
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new XUnitLoggerProvider(output));
            });
            ConfigureServices(services);
        });
    }

    [Fact]
    public async Task ConnectAsync_ConnectedToServer_ConnectedIsTureIdIsNotNullOrEmpty()
    {
        var io = NewSocketIO(Url);
        await io.ConnectAsync();

        io.Connected.Should().BeTrue();
        io.Id.Should().NotBeNullOrEmpty();
    }

    #region Emit

    [Fact]
    public async Task EmitAsync_EventNull_ReceiveNull()
    {
        var io = NewSocketIO(Url);
        IEventContext message = null!;
        io.On("1:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await io.ConnectAsync();
        await io.EmitAsync("1:emit", [null!]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        var receivedData = message.GetValue<object>(0);
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
        var io = NewSocketIO(Url);
        IEventContext message = null!;
        io.On("1:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await io.ConnectAsync();
        await io.EmitAsync("1:emit", [data]);

        await Task.Delay(DefaultDelay);

        // TODO: json?
        message.Should().NotBeNull();
        message.GetValue(data.GetType(), 0)
            .Should()
            .BeEquivalentTo(data);
    }

    [Fact]
    public async Task EmitAsync_ByteEvent1Parameter_ReceiveSameParameter()
    {
        var io = NewSocketIO(Url);
        IEventContext message = null!;
        io.On("1:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await io.ConnectAsync();
        await io.EmitAsync("1:emit", [TestFile.NiuB]);

        await Task.Delay(DefaultDelay * 2);

        message.Should().NotBeNull();
        message.GetValue<TestFile>(0)
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
        var io = NewSocketIO(Url);
        IEventContext message = null!;
        io.On("2:emit", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await io.ConnectAsync();
        await io.EmitAsync("2:emit", [item0, item1]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetValue(item0.GetType(), 0)
            .Should()
            .BeEquivalentTo(item0);
        message.GetValue(item1.GetType(), 1)
            .Should()
            .BeEquivalentTo(item1);
    }

    [Fact]
    public async Task EmitAsync_AckWith1StringParameter_ReceiveSameParameter()
    {
        var io = NewSocketIO(Url);
        IDataMessage message = null!;
        await io.ConnectAsync();
        await io.EmitAsync("1:ack", ["action"], msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });

        await Task.Delay(DefaultDelay * 2);

        message.Should().NotBeNull();
        message.GetValue<string>(0)
            .Should()
            .BeEquivalentTo("action");
    }

    [Fact]
    public async Task EmitAsync_AckWith1BinaryParameter_ReceiveSameParameter()
    {
        var io = NewSocketIO(Url);
        IDataMessage message = null!;
        await io.ConnectAsync();
        await io.EmitAsync("1:ack", [TestFile.NiuB], msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });

        await Task.Delay(DefaultDelay * 2);

        message.Should().NotBeNull();
        message.GetValue<TestFile>(0)
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
        var io = NewSocketIO(Url);
        var pingTimes = 0;
        var pongTimes = 0;
        io.OnPing += (_, _) => pingTimes++;
        io.OnPong += (_, _) => pongTimes++;

        await io.ConnectAsync();

        await Task.Delay(ms);
        pingTimes.Should().Be(expectedPingTimes);
        pongTimes.Should().Be(expectedPongTimes);
    }

    [Fact]
    public async Task ConnectAsync_ConnectAfterDisconnect_OnConnectedTimeIs2()
    {
        var io = NewSocketIO(Url);
        var times = 0;
        io.OnConnected += (_, _) => times++;

        await io.ConnectAsync();
        await io.DisconnectAsync();
        await io.ConnectAsync();

        await Task.Delay(100);

        times.Should().Be(2);
    }

    [Fact]
    public async Task SendAckDataAsync_ClientSend2Args_ServerExecuteCallback()
    {
        var io = NewSocketIO(Url);
        IEventContext message = null!;
        io.On("ack-on-client", async data =>
        {
            await data.SendAckDataAsync([1, 2]);
        });
        io.On("end-ack-on-client", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await io.ConnectAsync();
        await io.EmitAsync("begin-ack-on-client");

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetValue<int>(0).Should().Be(1);
        message.GetValue<int>(1).Should().Be(2);
    }

    [Fact]
    public async Task SendAckDataAsync_ClientSendBytes_ServerExecuteCallback()
    {
        var io = NewSocketIO(Url);
        IEventContext message = null!;
        io.On("ack-on-client", async data =>
        {
            await data.SendAckDataAsync([TestFile.IndexHtml, "hello"], CancellationToken.None);
        });
        io.On("end-ack-on-client", msg =>
        {
            message = msg;
            return Task.CompletedTask;
        });
        await io.ConnectAsync();
        await io.EmitAsync("begin-ack-on-client");

        await Task.Delay(DefaultDelay * 4);

        message.Should().NotBeNull();
        message.GetValue<TestFile>(0).Should().BeEquivalentTo(TestFile.IndexHtml);
        message.GetValue<string>(1).Should().Be("hello");
    }

    [Fact]
    public async Task ConnectAsync_InvalidQueryValue_InvokeOnError()
    {
        var errors = new List<string>();
        var io = NewSocketIO(TokenUrl);
        io.Options.Reconnection = false;
        io.Options.Query = new NameValueCollection
        {
            { "token", "invalid_token" }
        };
        io.OnError += (_, err) => errors.Add(err);

        await io
            .Invoking(async x => await x.ConnectAsync(CancellationToken.None))
            .Should()
            .ThrowExactlyAsync<ConnectionException>()
            .WithMessage("Authentication error");

        await Task.Delay(DefaultDelay / 10);

        io.Connected.Should().BeFalse();
        errors.Should().Equal("Authentication error");
    }

    [Fact]
    public async Task ConnectAsync_ValidQueryValue_ConnectSuccess()
    {
        var io = NewSocketIO(TokenUrl);
        io.Options.Reconnection = false;
        io.Options.Query = new NameValueCollection
        {
            { "token", "abc" }
        };

        await io.ConnectAsync(CancellationToken.None);

        io.Connected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_CalledByClient_OnDisconnectIsInvoked()
    {
        var io = NewSocketIO(Url);
        var times = 0;
        string? reason = null;
        io.OnDisconnected += (_, e) =>
        {
            times++;
            reason = e;
        };

        await io.ConnectAsync();
        await io.DisconnectAsync();

        times.Should().Be(1);
        reason.Should().Be(DisconnectReason.IOClientDisconnect);
        io.Id.Should().BeNull();
        io.Connected.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_CalledByServer_OnDisconnectIsInvoked()
    {
        var io = NewSocketIO(Url);
        var times = 0;
        string? reason = null;
        io.OnDisconnected += (_, e) =>
        {
            times++;
            reason = e;
        };

        await io.ConnectAsync();
        await io.EmitAsync("disconnect", [false]);
        await Task.Delay(100);

        times.Should().Be(1);
        reason.Should().Be(DisconnectReason.IOServerDisconnect);
        io.Id.Should().BeNull();
        io.Connected.Should().BeFalse();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public async Task Reconnect_Manually_OnConnectedAndOnDisconnectedTriggeredManyTimes(int times)
    {
        var io = NewSocketIO(Url);
        var connectTimes = 0;
        var disconnectTimes = 0;
        io.OnConnected += (_, _) => connectTimes++;
        io.OnDisconnected += (_, _) => disconnectTimes++;

        for (var i = 0; i < times; i++)
        {
            await io.ConnectAsync();
            await io.DisconnectAsync();
        }
        await Task.Delay(20);

        connectTimes.Should().Be(times);
        disconnectTimes.Should().Be(times);
    }

    [Theory]
    [InlineData("X-Custom-Header", "CustomHeader-Value")]
    [InlineData("User-Agent", "dotnet-socketio[client]/socket")]
    [InlineData("user-agent", "dotnet-socketio[client]/socket")]
    public async Task ExtraHeaders_UserGivenHeaders_PassThroughToServer(string key, string value)
    {
        var io = NewSocketIO(Url);
        string? actual = null;
        io.Options.ExtraHeaders = new Dictionary<string, string>
        {
            { key, value },
        };

        await io.ConnectAsync();
        await Task.Delay(DefaultDelay);
        var lowerCaseKey = key.ToLowerInvariant(); // limited by server
        await io.EmitAsync("get_header", [lowerCaseKey], res =>
        {
            actual = res.GetValue<string>(0);
            return Task.CompletedTask;
        });
        await Task.Delay(DefaultDelay);

        actual.Should().Be(value);
    }

    [Fact]
    public async Task OnAny_ReceivedEventMessage_HandlerIsCalled()
    {
        var io = NewSocketIO(Url);
        string? eventName = null;
        IEventContext context = null!;
        io.OnAny((e, ctx) =>
        {
            eventName = e;
            context = ctx;
            return Task.CompletedTask;
        });

        await io.ConnectAsync();
        await io.EmitAsync("1:emit", ["OnAny"]);
        await Task.Delay(100);

        eventName.Should().Be("1:emit");
        context.GetValue<string>(0).Should().Be("OnAny");
    }

    [Fact]
    public async Task OnAny_OnHandlerAndOnAnyHandler_2HandlersAreCalled()
    {
        var io = NewSocketIO(Url);
        var onHandlerCalled = false;
        var onAnyHandlerCalled = false;

        io.OnAny((_, _) =>
        {
            onAnyHandlerCalled = true;
            return Task.CompletedTask;
        });
        io.On("1:emit", _ =>
        {
            onHandlerCalled = true;
            return Task.CompletedTask;
        });

        await io.ConnectAsync();
        await io.EmitAsync("1:emit", ["OnAny"]);
        await Task.Delay(100);

        onHandlerCalled.Should().BeTrue();
        onAnyHandlerCalled.Should().BeTrue();
    }
}