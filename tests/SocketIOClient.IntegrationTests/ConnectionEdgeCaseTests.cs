using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common;
using SocketIOClient.Test.Core;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests;

public class ConnectionEdgeCaseTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(EngineIO.V4, TransportProtocol.WebSocket)]
    [InlineData(EngineIO.V4, TransportProtocol.Polling)]
    [InlineData(EngineIO.V3, TransportProtocol.WebSocket)]
    [InlineData(EngineIO.V3, TransportProtocol.Polling)]
    public async Task ServerCrashes_ConnectedChangeToFalseAndOnDiconnectedInvoked(EngineIO eio, TransportProtocol protocol)
    {
        const int port = 3001;
        using var process = StartServer(port, eio, protocol);
        using var io = NewSocketIO(port, new SocketIOOptions
        {
            EIO = eio,
            Transport = protocol,
            AutoUpgrade = false
        }, output);
        string reason = null!;
        io.OnDisconnected += (_, e) => reason = e;

        await io.ConnectAsync();
        io.Connected.Should().BeTrue();
        await Task.Delay(1000);

        process.Kill(true);
        await Task.Delay(5000);
        io.Connected.Should().BeFalse();
        reason.Should().Be("transport error");
    }

    private static Process StartServer(int port, EngineIO eio, TransportProtocol protocol)
    {
        var root = GetProjectRootDirectory();
        var version = eio == EngineIO.V3 ? "v2" : "v4";
        var workingDir = Path.Combine(root.FullName, "tests", "socket.io", version);
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "node",
            Arguments = "dynamic.js",
            WorkingDirectory = workingDir,
            Environment =
            {
                ["PORT"] = port.ToString(),
                ["TRANSPORT"] = protocol.ToString().ToLowerInvariant()
            }
        })!;

        return process;
    }

    private static DirectoryInfo GetProjectRootDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 16; i++)
        {
            if (dir.Name is "socket.io-client-csharp")
            {
                return dir;
            }

            dir = dir.Parent ?? throw new DirectoryNotFoundException();
        }

        throw new DirectoryNotFoundException();
    }

    private static SocketIO NewSocketIO(int port, SocketIOOptions options, ITestOutputHelper output)
    {
        var url = new Uri($"http://localhost:{port}");
        return new SocketIO(url, options, services =>
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new XUnitLoggerProvider(output));
            });
        });
    }
}