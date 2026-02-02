using System;
using SocketIOClient.Common;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.SystemJson;

public class WebSocketEngineIO3Tests(ITestOutputHelper output) : SocketIOTests(output)
{
    protected override Uri Url => new("http://localhost:11200");
    protected override Uri TokenUrl => new("http://localhost:11201");

    protected override SocketIOOptions Options => new()
    {
        EIO = EngineIO.V3,
        Transport = TransportProtocol.WebSocket,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}