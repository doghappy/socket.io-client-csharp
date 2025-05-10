using System;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Infrastructure;

public class RandomDelayRetryPolicy : IRetriable
{
    private readonly IRandom _random;

    public RandomDelayRetryPolicy(IRandom random)
    {
        _random = random;
    }

    public async Task RetryAsync(int times, Func<Task> func)
    {
        if (times < 1)
        {
            throw new ArgumentException("Times must be greater than 0", nameof(times));
        }
        for (var i = 1; i < times; i++)
        {
            try
            {
                await func();
                return;
            }
            catch
            {
                await Task.Delay(_random.Next(3));
            }
        }
        await func();
    }
}