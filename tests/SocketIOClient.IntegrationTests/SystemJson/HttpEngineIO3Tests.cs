using System;
using SocketIOClient.Common;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.SystemJson;

public class HttpEngineIO3Tests(ITestOutputHelper output) : SocketIOTests(output)
{
    // localhost.charlesproxy.com
    protected override Uri Url => new("http://localhost:11210");
    protected override Uri TokenUrl => new("http://localhost:11211");

    protected override SocketIOOptions Options => new()
    {
        EIO = EngineIO.V3,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}