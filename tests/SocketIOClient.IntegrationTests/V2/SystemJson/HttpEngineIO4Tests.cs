using System;
using SocketIOClient.Core;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2.SystemJson;

public class HttpEngineIO4Tests(ITestOutputHelper output) : SocketIOEngineIO4Tests(output)
{
    protected override Uri Url => new("http://localhost.charlesproxy.com:11410");
    protected override Uri TokenUrl => new("http://localhost:11411");

    protected override SocketIOClient.V2.SocketIOOptions Options => new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}