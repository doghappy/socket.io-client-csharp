using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Client.Transport;
using SocketIO.Core;

namespace SocketIO.Client.IntegrationTests;

[TestClass]
public class V4WebSocketSslTests
{
    [TestMethod]
    public async Task Should_ignore_SSL_error()
    {
        var io = new SocketIOClient("https://localhost:11404", new SocketIOOptions
        {
            EIO = EngineIO.V4,
            AutoUpgrade = false,
            Reconnection = false,
            Transport = TransportProtocol.WebSocket,
            ConnectionTimeout = TimeSpan.FromSeconds(2),
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        });
        var connected = false;
        io.OnConnected += (_, _) => connected = true;
        await io.ConnectAsync();

        connected.Should().BeTrue();
    }
}