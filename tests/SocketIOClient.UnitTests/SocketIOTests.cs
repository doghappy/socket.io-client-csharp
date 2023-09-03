using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SocketIOClient.Transport;

namespace SocketIOClient.UnitTests;

[TestClass]
public class SocketIOTests
{
    [TestMethod]
    public void EmitAckAction_InParallel_PacketIdShouldBeThreadSafe()
    {
        const int count = 100;
        var seq = new List<int>();
        using var io = new SocketIO("http://localhost");
        io.Transport = Substitute.For<ITransport>();

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
        io.Transport = Substitute.For<ITransport>();

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
}