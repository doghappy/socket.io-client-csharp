using System;
using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public class NoOpErrorStrategy : IErrorStrategy
{
    public Task OnErrorAsync(AggregateException _)
    {
        return Task.CompletedTask;
    }
}