using System.Diagnostics;
using FluentAssertions;
using SocketIOClient.Extensions;

namespace SocketIOClient.UnitTests.Extensions;

public class EventHandlerExtensionsTests
{
    [Fact(Skip = "Test")]
    public void RunInBackground_HandlerIsNull_DoNothing()
    {
        EventHandler<int> handler = null!;

        var action = () => handler.RunInBackground(this, 1);

        action.Should().NotThrow();
    }

    [Fact(Skip = "Test")]
    public void RunInBackground_ThrowException_NotBlocked()
    {
        EventHandler<int> handler = (_, _) => throw new Exception("test");

        var action = () => handler.RunInBackground(this, 1);

        action.Should().NotThrow();
    }

    [Fact(Skip = "Test")]
    public void RunInBackground_Delay100Ms_NotBlocked()
    {
        EventHandler<string> handler = (_, _) => Task.Delay(100).GetAwaiter().GetResult();

        var sw = Stopwatch.StartNew();
        handler.RunInBackground(this, "test");
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(80);
    }
}