using System;

namespace SocketIOClient.Infrastructure;

public interface IEventRunner
{
    void RunInBackground<T>(EventHandler<T>? handler, object sender, T args);
}