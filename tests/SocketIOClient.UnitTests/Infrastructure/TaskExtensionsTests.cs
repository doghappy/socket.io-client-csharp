using NSubstitute;
using SocketIOClient.Infrastructure;

namespace SocketIOClient.UnitTests.Infrastructure;

public class TaskExtensionsTests
{
    [Fact]
    public async Task FireAndForget_TaskDoesNotThrow_ErrorStrategyNotCalled()
    {
        var strategy = Substitute.For<IErrorStrategy>();

        var task = Task.CompletedTask;
        task.FireAndForget(strategy);
        await Task.Delay(200);

        await strategy.DidNotReceive().OnErrorAsync(Arg.Any<AggregateException>());
    }

    [Fact]
    public async Task FireAndForget_TaskThrows_ErrorStrategyWasCalled()
    {
        var strategy = Substitute.For<IErrorStrategy>();

        var task = Task.FromException(new Exception("Boom"));
        task.FireAndForget(strategy);
        await Task.Delay(200);

        await strategy.Received().OnErrorAsync(Arg.Any<AggregateException>());
    }
}