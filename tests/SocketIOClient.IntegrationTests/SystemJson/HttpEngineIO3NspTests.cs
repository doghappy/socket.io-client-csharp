using System;
using SocketIOClient.Common;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.SystemJson;

public class HttpEngineIO3NspTests(ITestOutputHelper output) : SocketIOTests(output)
{
    protected override Uri Url => new("http://localhost:11210/nsp");
    protected override Uri TokenUrl => new("http://localhost:11211/nsp");

    protected override SocketIOOptions Options => new()
    {
        EIO = EngineIO.V3,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}