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

    [Fact]
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

    [Theory]
    [InlineData(-1)]
    [InlineData(-0)]
    public void ReconnectionAttempts_SetInvalidValues_ThrowsException(int attempts)
    {
        var act = () => _options.ReconnectionAttempts = attempts;
        act.Should().Throw<ArgumentException>()
            .WithMessage("The minimum allowable number of attempts is 1");
    }
}