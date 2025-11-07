using FluentAssertions;
using SocketIOClient.Core;

namespace SocketIOClient.UnitTests.V2;

public class SocketIOOptionsTests
{
    public SocketIOOptionsTests()
    {
        _options = new SocketIOClient.V2.SocketIOOptions();
    }

    private readonly SocketIOClient.V2.SocketIOOptions _options;

    [Fact(Skip = "Test")]
    public void DefaultValues()
    {
        _options.Should()
            .BeEquivalentTo(new SocketIOClient.V2.SocketIOOptions
            {
                EIO = EngineIO.V4,
                ConnectionTimeout = TimeSpan.FromSeconds(30),
                Reconnection = true,
                ReconnectionAttempts = 10,
                ReconnectionDelayMax = 5000,
                Path = "/socket.io",
                Query = null,
            });
    }
}