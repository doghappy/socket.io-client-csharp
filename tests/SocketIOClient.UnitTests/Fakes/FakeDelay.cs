using SocketIOClient.Infrastructure;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Fakes;

public class FakeDelay(ITestOutputHelper output) : IDelay
{
    private readonly Queue<TaskCompletionSource> _delayTasks = [];

    public Task DelayAsync(int ms, CancellationToken cancellationToken)
    {
        output.WriteLine($"Delay {ms} ms enqueue...");
        var tcs = new TaskCompletionSource();
        _delayTasks.Enqueue(tcs);
        return tcs.Task;
    }

    public async Task AdvanceAsync(int count, TimeSpan timeout)
    {
        var i = 0;
        var ms = 0;
        const int delay = 20;
        while (i < count)
        {
            var hasTask = _delayTasks.TryDequeue(out var task);
            if (hasTask)
            {
                task!.SetResult();
                i++;
                continue;
            }

            output.WriteLine("Waiting for delay task enqueue...");
            await Task.Delay(delay);
            ms += delay;
            if (ms >= timeout.TotalMilliseconds)
            {
                return;
            }
        }
    }

    public async Task AdvanceAsync(int count)
    {
        await AdvanceAsync(count, TimeSpan.FromMinutes(1));
    }
}