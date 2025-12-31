using System;
using SocketIOClient.Core;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.SystemJson;

public class HttpEngineIO3NspTests(ITestOutputHelper output) : SocketIOTests(output)
{
    protected override Uri Url => new("http://localhost:11210/nsp");
    protected override Uri TokenUrl => new("http://localhost:11211/nsp");

    protected override SocketIOClient.V2.SocketIOOptions Options => new()
    {
        EIO = EngineIO.V3,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}