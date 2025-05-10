using System;
using System.Diagnostics;

namespace SocketIOClient.V2.Infrastructure;

public class SystemStopwatch : IStopwatch
{
    private readonly Stopwatch _stopwatch = new();

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Restart() => _stopwatch.Restart();

    public void Stop() => _stopwatch.Stop();
}