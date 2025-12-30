using System;
using SocketIOClient.Core;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2;

public class WebSocketEngineIO3NspTests(ITestOutputHelper output) : SocketIOTests(output)
{
    protected override Uri Url => new("http://localhost:11200/nsp");
    protected override Uri TokenUrl => new("http://localhost:11201/nsp");

    protected override SocketIOClient.V2.SocketIOOptions Options => new()
    {
        EIO = EngineIO.V3,
        Transport = TransportProtocol.WebSocket,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}