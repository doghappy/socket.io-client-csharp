using System;
using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public interface IRetriable
{
    Task RetryAsync(int times, Func<Task> func);
}