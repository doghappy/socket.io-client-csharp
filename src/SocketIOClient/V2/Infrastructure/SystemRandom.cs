using System;

namespace SocketIOClient.V2.Infrastructure;

public class SystemRandom : IRandom
{
    private readonly Random _random = new();

    public int Next(int max)
    {
        return _random.Next(max);
    }
}