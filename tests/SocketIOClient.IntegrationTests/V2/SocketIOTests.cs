using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2;

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

    [TestMethod]
    public async Task ConnectAsync_ConnectedToServer_ConnectedIsTureIdIsNotNullOrEmpty()
    {
        await _socket.ConnectAsync();

        _socket.Connected.Should().BeTrue();
        _socket.Id.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task EmitAsync_EventNull_ReceiveNull()
    {
        IAckMessage message = null!;
        _socket.On("1:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:emit", [null]);

        await Task.Delay(200);

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
        IAckMessage message = null!;
        _socket.On("1:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:emit", [data]);

        await Task.Delay(200);

        // TODO: json?
        message.Should().NotBeNull();
        message.GetDataValue(data.GetType(), 0)
            .Should()
            .BeEquivalentTo(data);
    }

    [TestMethod]
    public async Task EmitAsync_ByteEvent1Parameter_ReceiveSameParameter()
    {
        IAckMessage message = null!;
        _socket.On("1:emit", msg => message = msg);
        await _socket.ConnectAsync();
        await _socket.EmitAsync("1:emit", [TestFile.NiuB]);

        await Task.Delay(200);

        message.Should().NotBeNull();
        message.GetDataValue<TestFile>(0)
            .Should()
            .BeEquivalentTo(TestFile.NiuB);
    }
}