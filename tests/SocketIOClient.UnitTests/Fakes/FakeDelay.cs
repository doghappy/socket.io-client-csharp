using FluentAssertions;
using SocketIOClient.Infrastructure;
using Xunit.Abstractions;

namespace SocketIOClient.UnitTests.Fakes;

public class FakeDelay(ITestOutputHelper output) : IDelay
{
    private readonly Dictionary<int, List<TaskCompletionSource>> _delayTaskDic = [];
    private readonly object _lock = new();

    public Task DelayAsync(int ms, CancellationToken cancellationToken)
    {
        output.WriteLine($"Request delay {ms} ms...");
        var tcs = new TaskCompletionSource();
        lock (_lock)
        {
            var exists = _delayTaskDic.TryGetValue(ms, out var delayTasks);
            if (exists)
            {
                delayTasks!.Add(tcs);
            }
            else
            {
                _delayTaskDic[ms] = [tcs];
            }
        }
        return tcs.Task;
    }

    public async Task AdvanceAsync(int ms)
    {
        const int timeout = 5000;
        const int step = 20;
        var current = 0;
        while (true)
        {
            lock (_lock)
            {
                var exists = _delayTaskDic.TryGetValue(ms, out var tasks);
                if (exists)
                {
                    tasks![0].SetResult();
                    output.WriteLine($"Complete delay {ms} ms...");
                    tasks.RemoveAt(0);
                    if (tasks.Count == 0)
                    {
                        _delayTaskDic.Remove(ms);
                    }
                    return;
                }
            }

            if (current >= timeout)
            {
                throw new TimeoutException();
            }

            current += step;
            await Task.Delay(step).ConfigureAwait(false);
        }
    }

    public async Task AdvanceAsync(int ms, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await AdvanceAsync(ms).ConfigureAwait(false);
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
                lock (_lock)
                {
                    if (_delayTaskDic.Count > 0)
                    {
                        _delayTaskDic.Should().HaveCount(0);
                        return;
                    }
                }
                await Task.Delay(20, CancellationToken.None).ConfigureAwait(false);
            }
        }, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task EnsureNoDelayAsync(int ms)
    {
        await EnsureNoDelayAsync(TimeSpan.FromMilliseconds(ms)).ConfigureAwait(false);
    }
}