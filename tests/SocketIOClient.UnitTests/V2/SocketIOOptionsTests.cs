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
                Query = null,
                Transport = TransportProtocol.Polling,
                ExtraHeaders = null,
                Auth = null,
                AutoUpgrade = true
            });
        _options.Path.Should().BeNull();
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

    [Theory]
    [InlineData("test", "/test/")]
    [InlineData("test/", "/test/")]
    [InlineData("/test/", "/test/")]
    [InlineData("//test//", "/test/")]
    public void Path_SetNewValue_SurroundWithSlash(string path, string expected)
    {
        _options.Path = path;
        _options.Path.Should().Be(expected);
    }
}