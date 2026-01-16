using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using SocketIOClient.Core;
using SocketIOClient.Session.WebSocket;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.SystemJson;

public class HttpEngineIO4Tests(ITestOutputHelper output) : SocketIOEngineIO4Tests(output)
{
    protected override Uri Url => new("http://localhost:11410");
    protected override Uri TokenUrl => new("http://localhost:11411");

    protected override SocketIOOptions Options => new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };

    [Fact]
    public async Task ConnectAsync_WebSocketIsAvailable_UpgradeToWebSocket()
    {
        var uri = new Uri("http://localhost:11400");
        var io = NewSocketIO(uri);

        await io.ConnectAsync();

        io.Options.Transport.Should().Be(TransportProtocol.WebSocket);

        var prop = io.GetType().GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic);
        var session = prop!.GetValue(io);
        session.Should().BeOfType<WebSocketSession>();
    }
}