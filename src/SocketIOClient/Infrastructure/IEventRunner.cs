using System;

namespace SocketIOClient.Infrastructure;

public interface IEventRunner
{
    void RunInBackground(EventHandler? handler, object sender, EventArgs args);
    void RunInBackground<T>(EventHandler<T>? handler, object sender, T args);
}