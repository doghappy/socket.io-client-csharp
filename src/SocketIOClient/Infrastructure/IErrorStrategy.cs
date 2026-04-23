using System;
using System.Threading.Tasks;

namespace SocketIOClient.Infrastructure;

public interface IErrorStrategy
{
    Task OnErrorAsync(AggregateException ex);
}