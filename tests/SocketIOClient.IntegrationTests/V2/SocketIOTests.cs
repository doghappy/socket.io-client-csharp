using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.CommonTestData;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2;
using SocketIOClient.V2.Core;

namespace SocketIOClient.IntegrationTests.V2;

[TestClass]
public class SocketIOTests
{
    public SocketIOTests()
    {
        _socket = new SocketIOClient.V2.SocketIO("http://localhost:11210", new SocketIOClient.V2.SocketIOOptions
        {
            EIO = EngineIO.V3,
            Reconnection = false,
        });
    }

    private readonly SocketIOClient.V2.SocketIO _socket;
    public const int DefaultDelay = 200;

    [TestMethod]
    public async Task ConnectAsync_ConnectedToServer_ConnectedIsTureIdIsNotNullOrEmpty()
    {
        await _socket.ConnectAsync();

        _socket.Connected.Should().BeTrue();
        _socket.Id.Should().NotBeNullOrEmpty();
    }

    #region Emit

    [TestMethod]
    public async Task EmitAsync_EventNull_ReceiveNull()
    {
        IAckableMessage message = null!;
        _socket.On("1:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:emit", [null]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        var receivedData = message.GetDataValue<object>(0);
        receivedData.Should().BeNull();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    [DataRow(-1234567890)]
    [DataRow(1234567890)]
    [DataRow(-1.234567890)]
    [DataRow(1.234567890)]
    [DataRow("hello\n世界\n🌍🌎🌏")]
    public async Task EmitAsync_Event1Parameter_ReceiveSameParameter(object data)
    {
        IAckableMessage message = null!;
        _socket.On("1:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:emit", [data]);

        await Task.Delay(DefaultDelay);

        // TODO: json?
        message.Should().NotBeNull();
        message.GetDataValue(data.GetType(), 0)
            .Should()
            .BeEquivalentTo(data);
    }

    [TestMethod]
    public async Task EmitAsync_ByteEvent1Parameter_ReceiveSameParameter()
    {
        IAckableMessage message = null!;
        _socket.On("1:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:emit", [TestFile.NiuB]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<TestFile>(0)
            .Should()
            .BeEquivalentTo(TestFile.NiuB);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, 123)]
    [DataRow(-1234567890, "test")]
    [DataRow("hello\n世界\n🌍🌎🌏", 199)]
    public async Task EmitAsync_Event2Parameters_ReceiveSameParameters(object item0, object item1)
    {
        IAckableMessage message = null!;
        _socket.On("2:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("2:emit", [item0, item1]);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue(item0.GetType(), 0)
            .Should()
            .BeEquivalentTo(item0);
        message.GetDataValue(item1.GetType(), 1)
            .Should()
            .BeEquivalentTo(item1);
    }

    [TestMethod]
    public async Task EmitAsync_ActionAckWith1Parameter_ReceiveSameParameter()
    {
        IDataMessage message = null!;
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:ack", ["action"], msg => message = msg);

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<string>(0)
            .Should()
            .BeEquivalentTo("action");
    }

    [TestMethod]
    public async Task EmitAsync_FuncAckWith1Parameter_ReceiveSameParameter()
    {
        IDataMessage message = null!;
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:ack", [TestFile.NiuB], msg =>
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

    [TestMethod]
    [DataRow(4000, 0, 0)]
    [DataRow(5900, 1, 1)]
    [DataRow(14000, 2, 2)]
    public async Task OnPingAndOnPong_HandlerAreRegistered_WorkAsExpected(int ms, int expectedPingTimes, int expectedPongTimes)
    {
        var pingTimes = 0;
        var pongTimes = 0;
        _socket.OnPing += (_, _) => pingTimes++;
        _socket.OnPong += (_, _) => pongTimes++;

        await _socket.ConnectAsync();

        await Task.Delay(ms);
        pingTimes.Should().Be(expectedPingTimes);
        pongTimes.Should().Be(expectedPongTimes);
    }

    [TestMethod]
    public async Task ConnectAsync_ConnectAfterDisconnect_OnConnectedTimeIs2()
    {
        var times = 0;
        _socket.OnConnected += (_, _) => times++;

        await _socket.ConnectAsync();
        await _socket.DisconnectAsync();
        await _socket.ConnectAsync();

        await Task.Delay(100);

        times.Should().Be(2);
    }

    [TestMethod]
    public async Task SendAckDataAsync_ClientSend2Args_ServerExecuteCallback()
    {
        IAckableMessage message = null!;
        _socket.On("ack-on-client", async data =>
        {
            await data.SendAckDataAsync([1, 2]);
        });
        _socket.On("end-ack-on-client", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("begin-ack-on-client");

        await Task.Delay(DefaultDelay);

        message.Should().NotBeNull();
        message.GetDataValue<int>(0).Should().Be(1);
        message.GetDataValue<int>(1).Should().Be(2);
    }

    [TestMethod]
    public async Task SendAckDataAsync_ClientSendBytes_ServerExecuteCallback()
    {
        IAckableMessage message = null!;
        _socket.On("ack-on-client", async data =>
        {
            await data.SendAckDataAsync([TestFile.IndexHtml, "hello"], CancellationToken.None);
        });
        _socket.On("end-ack-on-client", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("begin-ack-on-client");

        await Task.Delay(DefaultDelay * 4);

        message.Should().NotBeNull();
        message.GetDataValue<TestFile>(0).Should().BeEquivalentTo(TestFile.IndexHtml);
        message.GetDataValue<string>(1).Should().Be("hello");
    }
}