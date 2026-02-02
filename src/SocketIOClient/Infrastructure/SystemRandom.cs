using System;

namespace SocketIOClient.Infrastructure;

public class SystemRandom : IRandom
{
    private readonly Random _random = new();

    public int Next(int max)
    {
        return _random.Next(max);
    }
}