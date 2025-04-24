using System;

namespace SocketIOClient.V2.Infrastructure;

public interface IStopwatch
{
    TimeSpan Elapsed { get; }
    void Restart();
    void Stop();
}