using System;
using SocketIOClient.Core;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2;

public class HttpEngineIO3Tests(ITestOutputHelper output) : SocketIOTests(output)
{
    // localhost.charlesproxy.com
    protected override Uri Url => new Uri("http://localhost:11210");
    protected override Uri TokenUrl => new Uri("http://localhost:11211");

    protected override SocketIOClient.V2.SocketIOOptions Options => new SocketIOClient.V2.SocketIOOptions
    {
        EIO = EngineIO.V3,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };
}