using System;

namespace SocketIOClient.Infrastructure;

public interface IStopwatch
{
    TimeSpan Elapsed { get; }
    void Restart();
    void Stop();
}