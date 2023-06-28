using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using SocketIO.Core;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class AutoReconnectTests
    {
        [TestMethod]
        [DataRow("v2", "v2-ws.js", 11292, EngineIO.V3, TransportProtocol.WebSocket)]
        [DataRow("v2", "v2-http.js", 11293, EngineIO.V3, TransportProtocol.Polling)]
        [DataRow("v4", "v4-ws.js", 11492, EngineIO.V4, TransportProtocol.WebSocket)]
        [DataRow("v4", "v4-http.js", 11493, EngineIO.V4, TransportProtocol.Polling)]
        public async Task Should_reconnect_when_server_shutdown(
            string folder,
            string name,
            int port,
            EngineIO eio,
            TransportProtocol transport)
        {
            using var tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync("localhost", port);
                throw new Exception($"Port '{port}' already in use");
            }
            catch
            {
                // ignored
            }

            var psi = new ProcessStartInfo("node")
            {
                Arguments = name,
                EnvironmentVariables =
                {
                    ["PORT"] = port.ToString()
                },
                WorkingDirectory = $"../../../../socket.io/{folder}"
            };
            using var process = Process.Start(psi);

            var isOpened = false;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    await tcpClient.ConnectAsync("localhost", port);
                    isOpened = true;
                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }

            isOpened.Should().BeTrue("the port '{0}' is not open.", port);

            var attemptTimes = 0;
            var reconnectedTimes = 0;
            using var io = new SocketIO($"http://127.0.0.1:{port}", new SocketIOOptions
            {
                Transport = transport,
                AutoUpgrade = false,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
                EIO = eio,
                Reconnection = true
            });
            io.OnReconnectAttempt += (_, attempts) => attemptTimes = attempts;
            io.OnReconnected += (_, _) => reconnectedTimes++;
            await io.ConnectAsync();

            io.Connected.Should().BeTrue();

            process!.Kill();
            process.WaitForExit(4000);
            await Task.Delay(4000);

            using var newProcess = Process.Start(psi);
            await Task.Delay(2000);

            attemptTimes.Should().BeGreaterThan(0);
            reconnectedTimes.Should().Be(1);
            newProcess!.Kill();
        }
    }
}