using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public async Task ConnectAsync_ConnectedToServer_IdAndConnectedHasValue()
    {
        await _socket.ConnectAsync();

        _socket.Connected.Should().BeTrue();
        _socket.Id.Should().NotBeNullOrEmpty();
    }
}