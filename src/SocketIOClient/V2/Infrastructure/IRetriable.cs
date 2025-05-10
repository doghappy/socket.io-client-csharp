using System;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Infrastructure;

public interface IRetriable
{
    Task RetryAsync(int times, Func<Task> func);
}