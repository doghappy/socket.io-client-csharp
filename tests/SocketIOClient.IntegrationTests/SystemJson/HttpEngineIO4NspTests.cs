using System;
using SocketIOClient.Common;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.SystemJson;

public class HttpEngineIO4NspTests(ITestOutputHelper output) : SocketIOEngineIO4Tests(output)
{
    protected override Uri Url => new("http://localhost:11410/nsp");
    protected override Uri TokenUrl => new("http://localhost:11411/nsp");

    protected override SocketIOOptions Options => new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}