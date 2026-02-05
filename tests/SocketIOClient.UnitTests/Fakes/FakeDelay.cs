using FluentAssertions;
using SocketIOClient.Infrastructure;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Fakes;

public class FakeDelay(ITestOutputHelper output) : IDelay
{
    private readonly Dictionary<int, List<TaskCompletionSource>> _delayTaskDic = [];

    public Task DelayAsync(int ms, CancellationToken cancellationToken)
    {
        output.WriteLine($"Delay {ms} ms enqueue...");
        var tcs = new TaskCompletionSource();
        var exists = _delayTaskDic.TryGetValue(ms, out var delayTasks);
        if (exists)
        {
            delayTasks!.Add(tcs);
        }
        else
        {
            _delayTaskDic[ms] = [tcs];
        }
        return tcs.Task;
    }

    public async Task AdvanceAsync(int ms)
    {
        while (true)
        {
            var exists = _delayTaskDic.TryGetValue(ms, out var tasks);
            if (exists)
            {
                tasks![0].SetResult();
                tasks.RemoveAt(0);
                if (tasks.Count == 0)
                {
                    _delayTaskDic.Remove(ms);
                }
                return;
            }
            await Task.Delay(20);
        }
    }

    private async Task EnsureNoDelayAsync(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        var token = cts.Token;
        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_delayTaskDic.Count > 0)
                {
                    _delayTaskDic.Should().HaveCount(0);
                    return;
                }
                await Task.Delay(20, CancellationToken.None);
            }
        }, CancellationToken.None);
    }

    public async Task EnsureNoDelayAsync(int ms)
    {
        await EnsureNoDelayAsync(TimeSpan.FromMilliseconds(ms));
    }
}