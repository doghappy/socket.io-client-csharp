using System;
using SocketIOClient.Core;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.SystemJson;

public class WebSocketEngineIO4Tests(ITestOutputHelper output) : SocketIOEngineIO4Tests(output)
{
    protected override Uri Url => new("http://localhost:11400");
    protected override Uri TokenUrl => new("http://localhost:11401");

    protected override SocketIOClient.V2.SocketIOOptions Options => new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.WebSocket,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}