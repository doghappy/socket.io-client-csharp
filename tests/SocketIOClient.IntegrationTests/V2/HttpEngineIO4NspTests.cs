using System;
using SocketIOClient.Core;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2;

public class HttpEngineIO4NspTests(ITestOutputHelper output) : SocketIOTests(output)
{
    protected override Uri Url => new("http://localhost:11410/nsp");
    protected override Uri TokenUrl => new("http://localhost:11411/nsp");

    protected override SocketIOClient.V2.SocketIOOptions Options => new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}