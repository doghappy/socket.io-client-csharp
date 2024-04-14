using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTests;

[TestClass]
public class SocketIOTests
{
    [TestMethod]
    public async Task EmitAckAction_InParallel_PacketIdShouldBeThreadSafe()
    {
        const int count = 100;
        var seq = new List<int>();
        using var io = new SocketIO("http://localhost");
        io.HttpClient = Substitute.For<IHttpClient>();
        io.HttpClient.ForE4ConnectAsync();
        await io.ConnectAsync();
        
        io.HttpClient.ForEmitAsync();
        Parallel.For(0, count, i =>
        {
            io.EmitAsync(i.ToString(), _ => seq.Add(i)).GetAwaiter().GetResult();
        });

        foreach (var v in io.AckActionHandlers.Values)
        {
            v(null);
        }

        io.AckActionHandlers.Should().HaveCount(count);
        io.AckActionHandlers.Keys.Should().BeEquivalentTo(seq);
    }
    
    
    [TestMethod]
    public async Task EmitAckFunc_InParallel_PacketIdShouldBeThreadSafe()
    {
        const int count = 100;
        var seq = new List<int>();
        using var io = new SocketIO("http://localhost");
        io.HttpClient = Substitute.For<IHttpClient>();
        io.HttpClient.ForE4ConnectAsync();
        await io.ConnectAsync();

        io.HttpClient.ForEmitAsync();
        Parallel.For(0, count, i =>
        {
            io.EmitAsync(i.ToString(), _ =>
            {
                seq.Add(i);
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
        });

        foreach (var v in io.AckFuncHandlers.Values)
        {
            await v(null);
        }

        io.AckFuncHandlers.Should().HaveCount(count);
        io.AckFuncHandlers.Keys.Should().BeEquivalentTo(seq);
    }

    [TestMethod]
    public async Task ConnectAsync_ShouldUpgradeToWebSocket()
    {
        using var io = new SocketIO("http://localhost", new SocketIOOptions
        {
            Reconnection = false
        });
        io.HttpClient = Substitute.For<IHttpClient>();
        io.HttpClient.ForUpgradeWebSocketAsync();

        var ws = Substitute.For<IClientWebSocket>();
        io.ClientWebSocketProvider = () => ws;
        ws.ForConnectAsync();
        
        await io.ConnectAsync();
        io.Options.Transport.Should().Be(TransportProtocol.WebSocket);
    }
}